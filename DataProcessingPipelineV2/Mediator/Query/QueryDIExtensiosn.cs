using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DataProcessingPipelineV2.Mediator.Query;

internal static class QueryDIExtensiosn
{
    public static IServiceCollection AddAllQueries
            (this IServiceCollection service, Assembly assembly)
    {
        var queries = assembly.GetTypes()
            .Where(x => !x.IsAbstract && x.IsClass
            && typeof(IQuery).IsAssignableFrom(x));
        foreach (var query in queries)
        {
            var queryInterface = query.GetInterfaces()
                .Where(i => !i.IsGenericType && typeof(IQuery) != i &&
                typeof(IQuery).IsAssignableFrom(i))
                .SingleOrDefault();
            if (queryInterface != null)
            {
                service.AddTransient(queryInterface, query);
            }
        }
        return service;
    }
}
