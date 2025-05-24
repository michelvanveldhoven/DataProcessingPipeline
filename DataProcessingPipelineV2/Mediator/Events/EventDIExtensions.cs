using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace DataProcessingPipelineV2.Mediator.Events;

internal static class EventDIExtensions
{
    public static IServiceCollection AddEventMediator(this IServiceCollection service)
    {
        service.AddTransient<IEventMediator, EventMediator>();
        return service;
    }

    public static IServiceCollection AddEventHandler<T, H>
            (this IServiceCollection services)
            where T : IEventNotification
            where H : class, IEventHandler<T>
    {
        services.AddScoped<H>();
        services.TryAddScoped(typeof(EventTrigger<>));

        return services;
    }

    public static IServiceCollection AddAllEventHandlers
            (this IServiceCollection service, Assembly assembly)
    {
        var method = typeof(EventDIExtensions).GetMethod("AddEventHandler",
            BindingFlags.Static | BindingFlags.Public);

        var handlers = assembly.GetTypes()
            .Where(x => !x.IsAbstract && x.IsClass
            && typeof(IEventHandler).IsAssignableFrom(x));
        foreach (var handler in handlers)
        {
            var handlerInterface = handler.GetInterfaces()
                .Where(i => i.IsGenericType && typeof(IEventHandler).IsAssignableFrom(i))
                .SingleOrDefault();
            if (handlerInterface != null)
            {
                var eventType = handlerInterface.GetGenericArguments()[0];
                method.MakeGenericMethod(new Type[] { eventType, handler })
                    .Invoke(null, new object[] { service });
            }
        }
        service.AddEventMediator();
        return service;
    }
}
