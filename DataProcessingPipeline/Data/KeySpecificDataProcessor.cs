using DataProcessingPipeline.Dependencies;
using System.Threading.Channels;

namespace DataProcessingPipeline.Data;

public class KeySpecificDataProcessor : IDataProcessor
{
    private DateTime? _processingFinishedTimestamp = DateTime.UtcNow;
    private bool _IsProcessing
    {
        set
        {
            if (!value)
            {
                _processingFinishedTimestamp = DateTime.UtcNow;
            }
            else
            {
                _processingFinishedTimestamp = null;
            }
        }
    }

    private ILogger _logger;
    private Task? _processingTask;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    public DateTime LastProcessingTimestamp => _processingFinishedTimestamp ?? DateTime.UtcNow;
    public int ProcessorKey { get; }

    

    private readonly Channel<DataWithKey> _internalQueue = Channel.CreateUnbounded<DataWithKey>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
    private KeySpecificDataProcessor(int processorKey, IServiceScopeFactory serviceScopeFactory, ILogger logger)
    {
        ProcessorKey = processorKey;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public static KeySpecificDataProcessor CreateAndStartProcessing(int processorKey, IServiceScopeFactory serviceScopeFactory, ILogger logger, CancellationToken processingCancellationToken = default)
    {
        var instance = new KeySpecificDataProcessor(processorKey, serviceScopeFactory, logger);
        instance.StartProcessing(processingCancellationToken);
        return instance;
    }

    public async Task ScheduleDataProcessing(DataWithKey data, CancellationToken cancellationToken = default)
    {
        if (data.Key != ProcessorKey)
        {
            throw new InvalidOperationException($"Data with key {data.Key} scheduled for KeySpecificDataProcessor with key {ProcessorKey}");
        }
        _IsProcessing = true;
        await _internalQueue.Writer.WriteAsync(data, cancellationToken);
    }

    public async Task StopProcessing()
    {
        _internalQueue.Writer.Complete();
        if (_processingTask is not null)
        {
            await _processingTask;
        }
    }

    private void StartProcessing(CancellationToken cancellationToken = default)
    {
        _processingTask = Task.Factory.StartNew(
            async () =>
            {
                await foreach (var data in _internalQueue.Reader.ReadAllAsync(cancellationToken))
                {
                    // TODO: Process data
                    _IsProcessing = true;
                    _logger?.LogInformation("Received data: {Data}", data);
                    using (var dependenciesProvider = new DependenciesProvider(_serviceScopeFactory))
                    {
                        await ProcessData(data, dependenciesProvider.Dependency);
                    }
                    _IsProcessing = _internalQueue.Reader.TryPeek(out _);
                }
            }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    private async Task ProcessData(DataWithKey data, IDependency dependency)
    {
        // TODO: Process data
        await dependency.DoStuff();
    }

    private class DependenciesProvider : IDisposable
    {
        private readonly IServiceScope _scope;

        public IDependency Dependency { get; }

        public DependenciesProvider(IServiceScopeFactory serviceScopeFactory)
        {
            _scope = serviceScopeFactory.CreateScope();
            Dependency = _scope.ServiceProvider.GetRequiredService<IDependency>();
        }

        public void Dispose()
        {
            _scope.Dispose();
        }
    }
}
