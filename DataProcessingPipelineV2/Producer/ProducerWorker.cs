using Microsoft.Extensions.Options;
using System.Threading.Channels;

namespace DataProcessingPipelineV2.Producer;

public class ProducerWorker([FromKeyedServices(ProducerQueue.WriterName)] ChannelWriter<ReceivedData> queuewriter,
    IOptions<ProducerQueueOptions> config,
    ILogger<ProducerWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var periodicTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(config.Value.TickInterval));
        while (!stoppingToken.IsCancellationRequested && await periodicTimer.WaitForNextTickAsync(stoppingToken)
            && await queuewriter.WaitToWriteAsync(stoppingToken))
        {
            var taskList = Enumerable.Range(1, Random.Shared.Next(2, 39))
                .Select(i => queuewriter.WriteAsync(Generate(i), stoppingToken).AsTask()).ToList();
            await Task.WhenAll(taskList);
            logger.LogInformation("Produced {Count} items", taskList.Count);
        }
    }

    private ReceivedData Generate(int i) => new ReceivedData(Random.Shared.Next(1, 9), $"de echte data van sleutel {Random.Shared.Next(1, 9)} instance  {i.ToString()}");
}
