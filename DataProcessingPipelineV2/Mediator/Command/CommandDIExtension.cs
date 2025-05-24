using System.Reflection;

namespace DataProcessingPipelineV2.Mediator.Command;

internal static class CommandDIExtension
{
    public static IServiceCollection AddAllCommandHandlers
            (this IServiceCollection service, Assembly assembly)
    {
        var handlers = assembly.GetTypes()
            .Where(x => !x.IsAbstract && x.IsClass
            && typeof(ICommandHandler).IsAssignableFrom(x));
        foreach (var handler in handlers)
        {
            var handlerInterface = handler.GetInterfaces()
                .Where(i => i.IsGenericType && typeof(ICommandHandler).IsAssignableFrom(i))
                .SingleOrDefault();
            if (handlerInterface != null)
            {
                service.AddScoped(handlerInterface, handler);
            }
        }
        return service;
    }
}
