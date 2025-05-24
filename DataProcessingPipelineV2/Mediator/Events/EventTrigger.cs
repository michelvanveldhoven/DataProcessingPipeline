namespace DataProcessingPipelineV2.Mediator.Events;

public class EventTrigger<T>(IEnumerable<IEventHandler<T>> handlers) where T : IEventNotification
{
    public async Task Trigger(T ev, CancellationToken cancellationToken = default)
    {
        List<Task> handlertasklist = [];
        foreach (var handler in handlers)
            handlertasklist = [..handlertasklist, handler.HandleAsync(ev, cancellationToken)];
        await Task.WhenAll(handlertasklist);
    }
}
