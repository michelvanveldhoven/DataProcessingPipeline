using System.Threading.Channels;

namespace DataProcessingPipelineV2.Monitor;

internal class ProducerMonitor(
    ILogger<ProducerMonitor> logger,
    [FromKeyedServices(ProducerQueue.Name)] Channel<ReceivedData> channel) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer ptimer = new(TimeSpan.FromSeconds(5));
        while (!stoppingToken.IsCancellationRequested && await ptimer.WaitForNextTickAsync(stoppingToken))
        {
            if (channel.Reader.CanCount)
            {
                var queueSize = channel.Reader.Count;
                logger.LogInformation("Producer Queue Size: {queueSize}", queueSize);
            }
            else
            {
                logger.LogWarning("Producer Queue Reader does not support counting.");
                await Task.Delay(TimeSpan.FromMinutes(5));
            }
        }
    }
}
