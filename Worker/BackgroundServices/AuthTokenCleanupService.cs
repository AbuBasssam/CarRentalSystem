using Interfaces;
using Serilog;
using Worker.Configuration;

namespace Worker.BackgroundServices;

/// <summary>
/// Background service that periodically cleans up expired auth tokens
/// Runs every 24 hours (configurable) and deletes tokens that have been expired
/// for more than the retention period (default: 7 days)
/// </summary>
public class AuthTokenCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly AuthTokenCleanupOptions _options;

    public AuthTokenCleanupService(
        IServiceScopeFactory serviceScopeFactory,
        AuthTokenCleanupOptions tokenCleanupOptions)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _options = tokenCleanupOptions;


        // Validate configuration on startup
        _options.Validate();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            Log.Information("Auth Token Cleanup Service is disabled in configuration");
            return;
        }

        Log.Information(
            "Auth Token Cleanup Service started. Running every {IntervalHours} hours, " +
            "retention period: {RetentionDays} days, batch size: {BatchSize}",
            _options.IntervalHours,
            _options.RetentionDaysAfterExpiry,
            _options.BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var delay = Helpers.CalculateNextRunDelay(
                    _options.RunAt,
                    TimeSpan.FromMinutes(_options.IntervalHours)
                );

                Log.Information(
                    "Next cleanup scheduled in {Hours} hours and {Minutes} minutes",
                    delay.Hours,
                    delay.Minutes);

                await Task.Delay(delay, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                    break;

                await ExecuteCleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                Log.Information("Auth Token Cleanup Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error in Auth Token Cleanup Service main loop");

                // Wait a bit before retrying to avoid rapid failure loops
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        Log.Information("Auth Token Cleanup Service stopped");
    }

    /// <summary>
    /// Executes the cleanup operation
    /// </summary>
    private async Task ExecuteCleanupAsync(CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;

        Log.Information("Starting Auth Token cleanup operation at {StartTime}", startTime);

        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var tokenRepository = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var totalDeleted = 0;
            var batchNumber = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                batchNumber++;

                // Get expired tokens for cleanup
                var expiredTokens = await tokenRepository.GetExpiredAuthTokensForCleanupAsync(
                    _options.RetentionDaysAfterExpiry,
                    _options.BatchSize);

                if (expiredTokens == null || !expiredTokens.Any())
                {
                    Log.Information("No expired auth tokens found for cleanup");
                    break;
                }

                Log.Debug(
                    "Batch {BatchNumber}: Found {Count} expired tokens to delete",
                    batchNumber,
                    expiredTokens.Count);

                // Delete the batch
                var deleted = tokenRepository.DeleteRange(expiredTokens);

                if (!deleted)
                {
                    Log.Warning("Failed to delete token batch {BatchNumber}", batchNumber);
                    break;
                }

                await unitOfWork.SaveChangesAsync(cancellationToken);

                totalDeleted += expiredTokens.Count;

                Log.Debug(
                    "Batch {BatchNumber}: Successfully deleted {Count} tokens",
                    batchNumber,
                    expiredTokens.Count);

                // If we got fewer tokens than batch size, we're done
                if (expiredTokens.Count < _options.BatchSize)
                {
                    break;
                }

                // Small delay between batches to avoid overwhelming the database
                await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
            }

            var duration = DateTime.UtcNow - startTime;

            Log.Information(
                "Auth Token cleanup completed. Total deleted: {TotalDeleted} tokens " +
                "in {Batches} batch(es). Duration: {Duration:mm\\:ss}",
                totalDeleted,
                batchNumber,
                duration);

            // Warning if operation took too long
            if (duration.TotalMinutes > 30)
            {
                Log.Warning(
                    "Cleanup operation took {Minutes} minutes, which is unusually long. " +
                    "Consider reducing batch size or investigating database performance",
                    duration.TotalMinutes);
            }
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;

            Log.Error(
                ex,
                "Error during Auth Token cleanup operation. Duration before failure: {Duration:mm\\:ss}",
                duration);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        Log.Information("Auth Token Cleanup Service is stopping gracefully");
        await base.StopAsync(cancellationToken);
    }
}
