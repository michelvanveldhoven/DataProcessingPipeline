using DataProcessingPipelineV2.Consumer;
using DataProcessingPipelineV2.Monitor;
using DataProcessingPipelineV2.Producer;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Threading.Channels;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

//DI injection of delegate
builder.Services.AddTransient<GetCurrentDateTime>(dt =>
    () => {
        return TimeProvider.System.GetUtcNow();
    });
builder.Services.AddSingleton<ProcessorFactory>();
builder.Services.AddScoped<IProcessor, Processor>();
builder.Services.AddHostedService<ConsumerWorker>();
builder.Services.Configure<ProducerQueueOptions>(_ => { });
builder.Services.AddHostedService<ProducerWorker>();
builder.Services.AddHostedService<ProducerMonitor>();

builder.Services.TryAddKeyedSingleton(ProducerQueue.Name, static (sp, key) => 
{
    var channel = Channel.CreateUnbounded<ReceivedData>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
    return channel;
});
builder.Services.TryAddKeyedSingleton(ProducerQueue.ReaderName, static (sp, key) =>
    sp.GetRequiredKeyedService<Channel<ReceivedData>>(ProducerQueue.Name).Reader);
builder.Services.TryAddKeyedSingleton(ProducerQueue.WriterName, static (sp, key) =>
    sp.GetRequiredKeyedService<Channel<ReceivedData>>(ProducerQueue.Name).Writer);

var host = builder.Build();
host.Run();


public record class ReceivedData(int key, string Data);

public enum ProducerQueue
{
    Name,
    ReaderName,
    WriterName
}

public class ProducerQueueOptions
{
    public int TickInterval { get; set; } = 10000; // in milliseconds
    public int MaxQueueSize { get; set; } = 1000;
    public int MaxDegreeOfParallelism { get; set; } = 4;
}

public delegate DateTimeOffset GetCurrentDateTime();