namespace DataProcessingPipelineV2.Mediator.Events;

public interface IEventHandler
{
}

public interface IEventHandler<T> : IEventHandler
where T : IEventNotification
{
    Task HandleAsync(T ev, CancellationToken cancellationToken = default);
}

public interface IEventNotification
{
}
