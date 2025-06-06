﻿using DataProcessingPipeline.Data;

namespace DataProcessingPipeline.Background;

public partial class BackgroundDataProcessor
{
    // This class is intended
    internal class BackgroundDataProcessorMonitor
    {
        private readonly TimeSpan _processorExpiryThreshold = TimeSpan.FromSeconds(30);

        private readonly TimeSpan _processorExpiryScanningPeriod = TimeSpan.FromSeconds(5);

        private MonitoringTask? _monitoringTask;

        private readonly SemaphoreSlim _processorsLock;

        private readonly Dictionary<int, KeySpecificDataProcessor> _dataProcessors;
        
        private readonly ILogger _logger;

        private BackgroundDataProcessorMonitor(SemaphoreSlim processorsLock, Dictionary<int, KeySpecificDataProcessor> dataProcessors, ILogger logger)
        {
            _processorsLock = processorsLock;
            _dataProcessors = dataProcessors;
            _logger = logger;
        }

        private void StartMonitoring(CancellationToken cancellationToken = default)
        {
            var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var task = Task.Factory.StartNew(async () =>
            {
                using var timer = new PeriodicTimer(_processorExpiryScanningPeriod);
                while (!tokenSource.IsCancellationRequested && await timer.WaitForNextTickAsync(tokenSource.Token))
                {
                    if (!await _processorsLock.WaitWithCancellation(cancellationToken: tokenSource.Token))
                    {
                        continue;
                    }

                    var expiredProcessors = _dataProcessors.Values.Where(IsExpired).ToList();
                    foreach (var expiredProcessor in expiredProcessors)
                    {
                        await expiredProcessor.StopProcessing();
                        _dataProcessors.Remove(expiredProcessor.ProcessorKey);
                        _logger.LogInformation("Removed expired processor with key {ProcessorKey}", expiredProcessor.ProcessorKey);
                    }

                    _processorsLock.Release();
                }
            }, tokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            _monitoringTask = new MonitoringTask(task, tokenSource);
        }

        private bool IsExpired(KeySpecificDataProcessor processor) => (DateTime.UtcNow - processor.LastProcessingTimestamp) > _processorExpiryThreshold;

        public async Task StopMonitoring()
        {
            if (_monitoringTask.HasValue)
            {
                if (!_monitoringTask.Value.CancellationTokenSource.IsCancellationRequested)
                {
                    _monitoringTask.Value.CancellationTokenSource.Cancel();
                }

                await _monitoringTask.Value.Task;
                _monitoringTask.Value.CancellationTokenSource.Dispose();
                _monitoringTask = null;
            }
        }

        public static BackgroundDataProcessorMonitor CreateAndStartMonitoring(SemaphoreSlim processorsLock, Dictionary<int, KeySpecificDataProcessor> dataProcessors, ILogger logger,CancellationToken monitoringCancellationToken = default)
        {
            var monitor = new BackgroundDataProcessorMonitor(processorsLock, dataProcessors, logger);
            monitor.StartMonitoring(monitoringCancellationToken);
            return monitor;
        }

        private readonly record struct MonitoringTask(Task Task, CancellationTokenSource CancellationTokenSource);

    }
}
