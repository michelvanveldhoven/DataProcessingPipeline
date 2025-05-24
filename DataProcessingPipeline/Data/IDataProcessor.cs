namespace DataProcessingPipeline.Data;

public interface IDataProcessor
{
    Task ScheduleDataProcessing(DataWithKey data, CancellationToken cancellationToken = default);
}
