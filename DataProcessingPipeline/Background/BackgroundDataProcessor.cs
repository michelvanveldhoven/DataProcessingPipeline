using DataProcessingPipeline.Data;
using System.Threading.Channels;

namespace DataProcessingPipeline.Background;

public partial class BackgroundDataProcessor(
    ILogger<BackgroundDataProcessor> logger, IServiceScopeFactory serviceScopeFactory) : BackgroundService, IDataProcessor
{
    private readonly Dictionary<int, KeySpecificDataProcessor> _dataProcessors = new();
    private Channel<DataWithKey> _dataChannel = Channel.CreateUnbounded<DataWithKey>(
        new UnboundedChannelOptions() { SingleReader = true });

    private readonly SemaphoreSlim _processorsLock = new(1, 1);
    private BackgroundDataProcessorMonitor? _monitor;

    public async Task ScheduleDataProcessing(DataWithKey data, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Scheduling data processing for key {Key}", data.Key);
        await _dataChannel.Writer.WriteAsync(data);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("BackgroundDataProcessor started at: {time}", DateTimeOffset.Now);
        _monitor = BackgroundDataProcessorMonitor.CreateAndStartMonitoring(_processorsLock, _dataProcessors, logger, stoppingToken);
        while (!stoppingToken.IsCancellationRequested && await _dataChannel.Reader.WaitToReadAsync(stoppingToken))
        {
            await foreach (var data in _dataChannel.Reader.ReadAllAsync(stoppingToken))
            {
                //TODO: Check if processor with key has lock
                if (!await _processorsLock.WaitWithCancellation(data.Key, stoppingToken))
                {
                    break;
                }
                // Create a processor for the data key if it doesn't exist
                // TODO: Get or create processor task
                var processor = GetOrCreateDataProcessor(data.Key);
                // TODO: Schedule data processing for that processor
                await processor.ScheduleDataProcessing(data, stoppingToken);

                _processorsLock.Release();
            }
        }

        await _monitor.StopMonitoring();
    }

    private KeySpecificDataProcessor GetOrCreateDataProcessor(int key, CancellationToken cancellationToken = default)
    {
        if (!_dataProcessors.TryGetValue(key, out var deviceProcessor))
        {
            var processor = CreateProcessor(key, cancellationToken);
            _dataProcessors[key] = processor;
            deviceProcessor = processor;
        }

        return deviceProcessor;
    }

    private KeySpecificDataProcessor CreateProcessor(int key, CancellationToken cancellationToken = default)
        => KeySpecificDataProcessor.CreateAndStartProcessing(key, serviceScopeFactory, logger, cancellationToken);
}
    