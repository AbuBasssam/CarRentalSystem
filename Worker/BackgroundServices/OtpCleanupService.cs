using Interfaces;
using Serilog;
using Worker.Configuration;

namespace Worker.BackgroundServices;

/// <summary>
/// Background service that periodically cleans up expired OTP codes
/// OTP Lifecycle:
/// - ConfirmEmail: 5 minutes validity
/// - ResetPassword: 3 minutes validity
/// 
/// Cleanup Policy:
/// - Used OTPs (IsUsed=1): delete after 1 hour
/// - Expired OTPs: delete after 1 hour from expiration
/// - Very old OTPs: delete after 24 hours regardless of status (safety fallback)
/// 
/// Runs every 15 minutes (configurable) to keep the database clean
/// </summary>
public class OtpCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly OtpCleanupOptions _options;

    public OtpCleanupService(IServiceScopeFactory serviceScopeFactory, OtpCleanupOptions cleanupOptions)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _options = cleanupOptions;

        // Validate configuration on startup
        _options.Validate();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            Log.Information("OTP Cleanup Service is disabled in configuration");

            return;
        }

        Log.Information(
            "OTP Cleanup Service started. Running every {IntervalMinutes} minutes, " +
            "retention period: {RetentionHours} hours, max age: {MaxAge} hours, batch size: {BatchSize}",
            _options.IntervalMinutes,
            _options.RetentionHoursAfterExpiry,
            _options.MaxAgeHours,
            _options.BatchSize
        );

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var delay = Helpers.CalculateNextRunDelay(
                    _options.RunAt,
                    TimeSpan.FromMinutes(_options.IntervalMinutes)
                );

                Log.Information("Next OTP cleanup scheduled in {Minutes} minutes", delay.TotalMinutes);

                await Task.Delay(delay, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                    break;

                await ExecuteCleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                Log.Information("OTP Cleanup Service is stopping");

                break;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error in OTP Cleanup Service main loop");

                // Wait a bit before retrying to avoid rapid failure loops
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        Log.Information("OTP Cleanup Service stopped");
    }

    /// <summary>
    /// Executes the cleanup operation
    /// </summary>
    private async Task ExecuteCleanupAsync(CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;

        Log.Information("Starting OTP cleanup operation at {StartTime}", startTime);

        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var otpRepository = scope.ServiceProvider.GetRequiredService<IOtpRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var totalDeleted = 0;
            var batchNumber = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                batchNumber++;

                // Get expired OTPs for cleanup
                var expiredOtps = await otpRepository.GetExpiredOtpsForCleanupAsync(
                    _options.RetentionHoursAfterExpiry,
                    _options.MaxAgeHours,
                    _options.BatchSize);

                if (expiredOtps == null || !expiredOtps.Any())
                {
                    Log.Information("No expired OTPs found for cleanup");
                    break;
                }

                Log.Debug(
                    "Batch {BatchNumber}: Found {Count} expired OTPs to delete",
                    batchNumber,
                    expiredOtps.Count
                );

                // Delete the batch
                var deleted = otpRepository.DeleteRange(expiredOtps);

                if (!deleted)
                {
                    Log.Warning("Failed to delete OTP batch {BatchNumber}", batchNumber);
                    break;
                }

                await unitOfWork.SaveChangesAsync(cancellationToken);

                totalDeleted += expiredOtps.Count;

                Log.Debug(
                    "Batch {BatchNumber}: Successfully deleted {Count} OTPs",
                    batchNumber,
                    expiredOtps.Count
                );

                // If we got fewer OTPs than batch size, we're done
                if (expiredOtps.Count < _options.BatchSize)
                {
                    break;
                }

                // Small delay between batches to avoid overwhelming the database
                await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
            }

            var duration = DateTime.UtcNow - startTime;

            Log.Information(
                "OTP cleanup completed. Total deleted: {TotalDeleted} OTPs " +
                "in {Batches} batch(es). Duration: {Duration:mm\\:ss}",
                totalDeleted,
                batchNumber,
                duration
            );

            // Warning if operation took too long (OTP cleanup should be very fast)
            if (duration.TotalMinutes > 5)
            {
                Log.Warning(
                    "Cleanup operation took {Minutes} minutes, which is unusually long. " +
                    "Consider investigating database performance",
                    duration.TotalMinutes
                );
            }
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;

            Log.Error(ex, "Error during OTP cleanup operation. Duration before failure: {Duration:mm\\:ss}", duration);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        Log.Information("OTP Cleanup Service is stopping gracefully");

        await base.StopAsync(cancellationToken);
    }
}