using Interfaces;
using Serilog;
using Worker.Configuration;

namespace Worker.BackgroundServices;

/// <summary>
/// Background service that automatically deletes unverified user accounts
/// Removes users who haven't confirmed their email within the allowed time period
/// This frees up usernames/emails and maintains database cleanliness
/// 
/// Runs every IntervalHours (default: 12 hours) or at a specific time if RunAt is configured
/// </summary>
public class UnverifiedUserCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly UnverifiedUserCleanupOptions _options;

    public UnverifiedUserCleanupService(IServiceScopeFactory serviceScopeFactory, UnverifiedUserCleanupOptions options)
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
            Log.Information("Unverified User Cleanup Service is disabled in configuration");
            return;
        }

        Log.Information(
            "Unverified User Cleanup Service started. Running every {IntervalHours} hours, " +
            "retention period: {RetentionHours} hours, batch size: {BatchSize}",
            _options.IntervalHours,
            _options.UnverifiedRetentionHours,
            _options.BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var delay = CalculateNextRunDelay();

                Log.Information(
                    "Next Unverified User cleanup scheduled in {Hours} hours and {Minutes} minutes",
                    delay.Hours,
                    delay.Minutes);

                await Task.Delay(delay, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                    break;

                await ExecuteCleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                Log.Information("Unverified User Cleanup Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error in Unverified User Cleanup Service main loop");

                // Wait before retrying to avoid rapid failure loops
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        Log.Information("Unverified User Cleanup Service stopped");
    }

    /// <summary>
    /// Executes the cleanup operation
    /// Deletes unverified users that exceeded the retention period
    /// </summary>
    private async Task ExecuteCleanupAsync(CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;

        Log.Information("Starting Unverified User cleanup operation at {StartTime}", startTime);

        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();


            var batchNumber = 0;
            var totalDeleted = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                batchNumber++;

                // Get unverified users for deletion
                var unverifiedUsers = await userRepository.GetUnverifiedUsersForDeletionAsync(_options.UnverifiedRetentionHours, _options.BatchSize);

                if (unverifiedUsers == null || !unverifiedUsers.Any())
                {
                    Log.Information("No unverified users found for deletion");
                    break;
                }

                Log.Debug(
                    "Batch {BatchNumber}: Found {Count} unverified users to delete",
                    batchNumber,
                    unverifiedUsers.Count
                );

                // Delete users one by one (UserManager handles cascade delete)
                bool deleted = userRepository.DeleteRange(unverifiedUsers);
                if (!deleted)
                {
                    Log.Warning("Failed to delete  unverified users {BatchNumber}", batchNumber);
                    break;
                }
                await unitOfWork.SaveChangesAsync(cancellationToken);
                totalDeleted += unverifiedUsers.Count;



                Log.Debug(
                    "Batch {BatchNumber}: Deleted {Deleted} users",
                    batchNumber,
                    unverifiedUsers.Count
                );

                // If we got fewer users than batch size, we're done
                if (unverifiedUsers.Count < _options.BatchSize)
                {
                    break;
                }

                // Small delay between batches to avoid overwhelming the database
                await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);
            }

            var duration = DateTime.UtcNow - startTime;

            Log.Information(
                "Unverified User cleanup completed. " +
                "Total deleted: {TotalDeleted}" +
                "Batches: {Batches}, Duration: {Duration:mm\\:ss}",
                totalDeleted,
                batchNumber,
                duration
            );

            // Warning if operation took too long
            if (duration.TotalMinutes > 30)
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
                "Error during Unverified User cleanup operation. " +
                "Duration before failure: {Duration:mm\\:ss}",
                duration);
        }
    }

    /// <summary>
    /// Calculates the delay until the next run
    /// If RunAt time is specified, calculates delay to that time
    /// Otherwise, uses the IntervalHours setting
    /// </summary>
    private TimeSpan CalculateNextRunDelay()
    {
        if (string.IsNullOrEmpty(_options.RunAt))
        {
            return TimeSpan.FromHours(_options.IntervalHours);
        }

        var now = DateTime.UtcNow;
        var scheduledTime = TimeOnly.Parse(_options.RunAt);
        var todayScheduled = now.Date.Add(scheduledTime.ToTimeSpan());

        // If today's scheduled time has passed, schedule for tomorrow
        var nextRun = todayScheduled > now
            ? todayScheduled
            : todayScheduled.AddDays(1);

        var delay = nextRun - now;

        return delay > TimeSpan.Zero ? delay : TimeSpan.FromHours(_options.IntervalHours);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        Log.Information("Unverified User Cleanup Service is stopping gracefully");
        await base.StopAsync(cancellationToken);
    }
}