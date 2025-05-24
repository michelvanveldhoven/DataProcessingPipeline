using DataProcessingPipeline.Data;

namespace DataProcessingPipeline;

public class ProducerWorker(ILogger<ProducerWorker> logger,IDataProcessor _dataProcessor) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(3400, stoppingToken); // Initial delay to ensure the service is ready  
        logger.LogInformation("Producer Worker started at: {time}", DateTimeOffset.Now);
        while (!stoppingToken.IsCancellationRequested)
        {
            await _dataProcessor.ScheduleDataProcessing(new DataWithKey(1, "Data 1.1"));
            await _dataProcessor.ScheduleDataProcessing(new DataWithKey(2, "Data 2.1"));
            await _dataProcessor.ScheduleDataProcessing(new DataWithKey(1, "Data 1.2"));
            await _dataProcessor.ScheduleDataProcessing(new DataWithKey(3, "Data 3.1"));
            await _dataProcessor.ScheduleDataProcessing(new DataWithKey(1, "Data 1.3"));
            await Task.Delay(10000, stoppingToken);
            await _dataProcessor.ScheduleDataProcessing(new DataWithKey(2, "Data 2.2"));
            await Task.Delay(10000, stoppingToken);
            await _dataProcessor.ScheduleDataProcessing(new DataWithKey(3, "Data 3.2"));
            await Task.Delay(35000, stoppingToken);
            await _dataProcessor.ScheduleDataProcessing(new DataWithKey(3, "Data 3.3"));
            await Task.Delay(35000, stoppingToken);
        }
    }
}
