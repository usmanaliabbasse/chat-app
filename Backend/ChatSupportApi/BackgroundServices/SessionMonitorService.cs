using ChatSupportApi.Services;

namespace ChatSupportApi.BackgroundServices
{
    public class SessionMonitorService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SessionMonitorService> _logger;

        public SessionMonitorService(
            IServiceProvider serviceProvider,
            ILogger<SessionMonitorService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Session Monitor Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var queueManagementService = scope.ServiceProvider
                            .GetRequiredService<IQueueManagementService>();

                        await queueManagementService.MonitorInactiveSessions();
                    }

                    // Check every 2 seconds
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Session Monitor Service");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }

            _logger.LogInformation("Session Monitor Service stopped");
        }
    }
}
