using Hangfire;
using System.Diagnostics;

namespace KeepAwake;

public class Worker(ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ConfigureJobs();

        while (!stoppingToken.IsCancellationRequested)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }
            await Task.Delay(1000, stoppingToken);
        }
    }

    public void ConfigureJobs()
    {
        RecurringJobOptions jobOptions = new RecurringJobOptions();
        jobOptions.TimeZone = TimeZoneInfo.Local;

        RecurringJob.AddOrUpdate(
            "sleep-on-6pm",           // Unique identifier for the job
            () => TurnSleepOn(),        // Job logic
            "0 18 * * 1-5",
            jobOptions
        );

        RecurringJob.AddOrUpdate(
            "sleep-off-7am",            // Unique identifier for the job
            () => TurnSleepOff(), // Job logic
            "0 7 * * 1-5",
            jobOptions
        );
    }

    public void TurnSleepOn()
    {
        ExecuteSleepCommands(new Dictionary<string, int>
        {
            { "monitor-timeout-ac", 5 },
            { "standby-timeout-ac", 10 }
        });

        logger.LogInformation("Sleep turned on at {Time}", DateTimeOffset.Now);
    }

    public void TurnSleepOff()
    {
        ExecuteSleepCommands(new Dictionary<string, int>
        {
            { "monitor-timeout-ac", 0 },
            { "standby-timeout-ac", 0 }
        });

        logger.LogInformation("Sleep turned off at {Time}", DateTimeOffset.Now);
    }

    private void ExecuteSleepCommands(Dictionary<string, int> settings)
    {
        foreach (var (setting, value) in settings)
        {
            ProcessCommand($"/change {setting} {value}");
        }
    }

    private void ProcessCommand(string arguments)
    {
        var processStartInfo = new ProcessStartInfo
        {
            UseShellExecute = false,
            FileName = "powercfg",
            Arguments = arguments,
            CreateNoWindow = true,
            Verb = "runas" // Ensures the command runs with elevated privileges
        };

        try
        {
            using var process = Process.Start(processStartInfo);
            process!.WaitForExit(); // Wait for the command to complete

            if (process.ExitCode == 0)
            {
                logger.LogInformation("Successfully executed: {Arguments} at {Time}", arguments, DateTimeOffset.Now);
            }
            else
            {
                logger.LogWarning("Command executed with non-zero exit code: {Arguments}, ExitCode: {ExitCode}", arguments, process.ExitCode);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing power command: {Arguments}", arguments);
        }
    }
}

