using ChatSupportApi.Services;

namespace ChatSupportApi.BackgroundServices
{
    public class QueueProcessorService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<QueueProcessorService> _logger;

        public QueueProcessorService(
            IServiceProvider serviceProvider,
            ILogger<QueueProcessorService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Queue Processor Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var agentAssignmentService = scope.ServiceProvider
                            .GetRequiredService<IAgentAssignmentService>();

                        await agentAssignmentService.ProcessQueue();
                    }

                    // Process queue every 3 seconds
                    await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Queue Processor Service");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }

            _logger.LogInformation("Queue Processor Service stopped");
        }
    }
}
