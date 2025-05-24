using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DataProcessingPipelineV2.Consumer;

public class ConsumerWorker(
    ILogger<ConsumerWorker> logger,
    [FromKeyedServices(ProducerQueue.ReaderName)] ChannelReader<ReceivedData> channelReader,
    IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        while (!stoppingToken.IsCancellationRequested && await channelReader.WaitToReadAsync(stoppingToken))
        {
            await foreach (var item in channelReader.ReadAllAsync(stoppingToken))
            {
                //using var scope = serviceScopeFactory.CreateScope();
                var factory = scope.ServiceProvider.GetRequiredService<ProcessorFactory>();
                var processor = factory.GetProcessor(item.key,scope);
                await processor.ProcessAsync(item, stoppingToken);

                //logger.LogInformation("Received data: {Data}", item.Data);
                // Simulate processing time
                await Task.Delay(TimeSpan.FromMilliseconds(100), stoppingToken);
            }
        }
    }
}


public interface IProcessor
{
    void SetKey(int key);
    Task ProcessAsync(ReceivedData data, CancellationToken cancellationToken);
}


public class Processor(ILogger<Processor> logger) : IProcessor
{
    private int _key;

    public void SetKey(int key)
    {
        if (_key == 0)
        {
            _key = key;
        }
        
    }   

    public Task ProcessAsync(ReceivedData data, CancellationToken cancellationToken)
    {
        // Process data of type A
        logger.LogInformation("Processing data of type A: {Key} --> {Data}", data.key, data.Data);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Create new processors based on the type of data received.
/// </summary>
public class ProcessorFactory()
{
    public Dictionary<int, IProcessor> _proccesors = new Dictionary<int, IProcessor>();

    public IProcessor GetProcessor(int processorForKey, IServiceScope serviceScopeFactory)
    {
        if (_proccesors.TryGetValue(processorForKey, out var processor))
        {
            return processor;
        }

        // Create a new processor based on the data type
        // This is just an example, you would implement your own logic here
        processor = serviceScopeFactory.ServiceProvider.GetRequiredService<IProcessor>();
        processor.SetKey(processorForKey);
        _proccesors[processorForKey] = processor;
        return processor;
    }
}