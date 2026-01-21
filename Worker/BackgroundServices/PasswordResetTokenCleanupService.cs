using Interfaces;
using Serilog;
using Worker.Configuration;

namespace Worker.BackgroundServices;

/// <summary>
/// Background service that manages password reset token lifecycle
/// Performs two main operations:
/// 1. Auto-revokes expired tokens (tokens older than TokenValidityMinutes)
/// 2. Permanently deletes old revoked tokens (after RetentionDaysAfterExpiry)
/// 
/// Runs every IntervalHours (default: 6 hours) or at a specific time if RunAt is configured
/// </summary>
public class PasswordResetTokenCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly PasswordResetTokenCleanupOptions _options;

    public PasswordResetTokenCleanupService(IServiceScopeFactory serviceScopeFactory, PasswordResetTokenCleanupOptions options)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _options = options;

        // Validate configuration on startup
        _options.Validate();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            Log.Information("Password Reset Token Cleanup Service is disabled in configuration");
            return;
        }

        Log.Information(
            "Password Reset Token Cleanup Service started. Running every {IntervalHours} hours, " +
            "token validity: {ValidityMinutes} minutes, retention period: {RetentionDays} days, " +
            "batch size: {BatchSize}",
            _options.IntervalHours,
            _options.TokenValidityMinutes,
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
                    "Next Password Reset Token cleanup scheduled in {Hours} hours and {Minutes} minutes",
                    delay.Hours,
                    delay.Minutes
                );

                await Task.Delay(delay, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                    break;

                await ExecuteCleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                Log.Information("Password Reset Token Cleanup Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error in Password Reset Token Cleanup Service main loop");

                // Wait before retrying to avoid rapid failure loops
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        Log.Information("Password Reset Token Cleanup Service stopped");
    }

    /// <summary>
    /// Executes the complete cleanup operation:
    /// Phase 1: Revoke expired tokens
    /// Phase 2: Delete old revoked tokens
    /// </summary>
    private async Task ExecuteCleanupAsync(CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;

        Log.Information("Starting Password Reset Token cleanup operation at {StartTime}", startTime);

        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var tokenRepository = scope.ServiceProvider.GetRequiredService<IUserTokenRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            // Phase 1: Auto-revoke expired tokens
            var revokedCount = await RevokeExpiredTokensAsync(tokenRepository, unitOfWork, cancellationToken);

            // Phase 2: Delete old revoked tokens
            var deletedCount = await DeleteOldTokensAsync(tokenRepository, unitOfWork, cancellationToken);

            var duration = DateTime.UtcNow - startTime;

            Log.Information(
                "Password Reset Token cleanup completed. " +
                "Revoked: {RevokedCount} tokens, Deleted: {DeletedCount} tokens. " +
                "Duration: {Duration:mm\\:ss}",
                revokedCount,
                deletedCount,
                duration
            );

            // Warning if operation took too long
            if (duration.TotalMinutes > 15)
            {
                Log.Warning(
                    "Cleanup operation took {Minutes} minutes, which is unusually long. " +
                    "Consider reducing batch size or investigating database performance",
                    duration.TotalMinutes
                );
            }
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;

            Log.Error(
                ex,
                "Error during Password Reset Token cleanup operation. " +
                "Duration before failure: {Duration:mm\\:ss}",
                duration
            );
        }
    }

    /// <summary>
    /// Phase 1: Revokes tokens that have exceeded their validity period
    /// Tokens are revoked (not deleted) to maintain audit trail
    /// </summary>
    private async Task<int> RevokeExpiredTokensAsync(IUserTokenRepository tokenRepository, IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        Log.Information("Phase 1: Starting auto-revocation of expired password reset tokens");

        var totalRevoked = 0;
        var batchNumber = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            batchNumber++;

            // Get tokens that exceeded validity period but not yet revoked
            var expiredTokens = await tokenRepository.GetExpiredPasswordResetTokensAsync(
                _options.TokenValidityMinutes,
                _options.BatchSize
            );

            if (expiredTokens == null || !expiredTokens.Any())
            {
                Log.Information("Phase 1: No expired password reset tokens found for revocation");
                break;
            }

            Log.Debug(
                "Phase 1 - Batch {BatchNumber}: Found {Count} expired tokens to revoke",
                batchNumber,
                expiredTokens.Count
            );

            // Revoke each token in the batch
            foreach (var token in expiredTokens)
            {
                token.Revoke();
                token.ForceExpire();
            }

            // Update the batch
            var updated = tokenRepository.UpdateRange(expiredTokens);

            if (!updated)
            {
                Log.Warning("Phase 1: Failed to revoke token batch {BatchNumber}", batchNumber);
                break;
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            totalRevoked += expiredTokens.Count;

            Log.Debug(
                "Phase 1 - Batch {BatchNumber}: Successfully revoked {Count} tokens",
                batchNumber,
                expiredTokens.Count);

            // If we got fewer tokens than batch size, we're done
            if (expiredTokens.Count < _options.BatchSize)
            {
                break;
            }

            // Small delay between batches
            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
        }

        Log.Information("Phase 1 completed: Total revoked {TotalRevoked} tokens", totalRevoked);

        return totalRevoked;
    }

    /// <summary>
    /// Phase 2: Permanently deletes revoked tokens that exceeded retention period
    /// This cleans up old audit data to save database space
    /// </summary>
    private async Task<int> DeleteOldTokensAsync(IUserTokenRepository tokenRepository, IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        Log.Information("Phase 2: Starting deletion of old password reset tokens");

        var totalDeleted = 0;
        var batchNumber = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            batchNumber++;

            // Get revoked tokens that exceeded retention period
            var oldTokens = await tokenRepository.GetOldPasswordResetTokensAsync(
                _options.RetentionDaysAfterExpiry,
                _options.BatchSize
            );

            if (oldTokens == null || !oldTokens.Any())
            {
                Log.Information("Phase 2: No old password reset tokens found for deletion");
                break;
            }

            Log.Debug(
                "Phase 2 - Batch {BatchNumber}: Found {Count} old tokens to delete",
                batchNumber,
                oldTokens.Count
            );

            // Delete the batch
            var deleted = tokenRepository.DeleteRange(oldTokens);

            if (!deleted)
            {
                Log.Warning("Phase 2: Failed to delete token batch {BatchNumber}", batchNumber);
                break;
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            totalDeleted += oldTokens.Count;

            Log.Debug(
                "Phase 2 - Batch {BatchNumber}: Successfully deleted {Count} tokens",
                batchNumber,
                oldTokens.Count
            );

            // If we got fewer tokens than batch size, we're done
            if (oldTokens.Count < _options.BatchSize)
            {
                break;
            }

            // Small delay between batches
            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
        }

        Log.Information("Phase 2 completed: Total deleted {TotalDeleted} tokens", totalDeleted);

        return totalDeleted;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        Log.Information("Password Reset Token Cleanup Service is stopping gracefully");
        await base.StopAsync(cancellationToken);
    }
}