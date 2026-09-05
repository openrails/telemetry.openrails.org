using System.Globalization;

namespace Open_Rails_Telemetry.Services
{
    public static class BackgroundDataMigrationExtensions
    {
        public static IServiceCollection AddBackgroundDataMigration(this IServiceCollection services) => services.AddHostedService<BackgroundDataMigration>();
    }

    public class BackgroundDataMigration : BackgroundService
    {
        readonly ILogger<BackgroundDataMigration> Logger;
        readonly IConfiguration Configuration;
        readonly string DataPathCollectSystem;

        public BackgroundDataMigration(ILogger<BackgroundDataMigration> logger, IConfiguration configuration)
        {
            Logger = logger;
            Configuration = configuration;
            DataPathCollectSystem = Path.Combine(Configuration["DataPath"], "collect", "system");
            Directory.CreateDirectory(DataPathCollectSystem);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                await Update();
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        async Task Update()
        {
            foreach (var file in Directory.EnumerateFiles(DataPathCollectSystem, "*.json"))
            {
                if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebug("Inspect: {file}", file);
                var name = Path.GetFileName(file);
                if (name.Length == 52 && name[10] == '_' && DateTime.TryParseExact(name[..10], "yyyy-MM-dd", null, DateTimeStyles.None, out var date))
                {
                    var directory = Path.Combine(DataPathCollectSystem, ISOWeek.GetYear(date).ToString(), "W" + ISOWeek.GetWeekOfYear(date).ToString("00"));
                    Directory.CreateDirectory(directory);
                    File.Move(file, Path.Combine(directory, name));
                    if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebug("Moved: {file}", file);
                    break;
                }
            }
        }
    }
}
