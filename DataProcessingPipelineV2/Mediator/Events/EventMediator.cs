namespace DataProcessingPipelineV2.Mediator.Events;

public interface IEventMediator
{
    Task TriggerEvents(IEnumerable<IEventNotification> events);
}

public class EventMediator : IEventMediator
{
    IServiceProvider services;
    public EventMediator(IServiceProvider services)
    {
        this.services = services;
    }
    public async Task TriggerEvents(IEnumerable<IEventNotification> events)
    {
        if (events == null) return;
        foreach (var ev in events)
        {
            var triggerType = typeof(EventTrigger<>).MakeGenericType(ev.GetType());
            var trigger = services.GetRequiredService(triggerType);
            var task = (Task)triggerType.GetMethod(nameof(EventTrigger<IEventNotification>.Trigger))
                .Invoke(trigger, new object[] { ev })!;
            await task;
        }
    }
}