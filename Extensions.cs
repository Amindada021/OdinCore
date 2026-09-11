using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace OdinCore;

public static class Extensions
{
    private static bool Include(this Type? @this, Type odin)
    {
        if (@this == null || !@this.IsClass)
        {
            return false;
        }

        if (@this.Namespace == odin.Namespace && @this.Name == odin.Name)
        {
            return true;
        }

        return @this.BaseType.Include(odin);
    }

    public static IServiceCollection AddOdin(this IServiceCollection services, Assembly? assembly)
    {
        if (assembly == null)
        {
            throw new ArgumentNullException("assembly");
        }

        foreach (ServiceDescriptor item in from t in assembly.GetTypes()
                                           where t.BaseType.Include(typeof(Odin<,>))
                                           select new ServiceDescriptor(t, t, t.GetCustomAttribute<ServiceLifetimeAttribute>()?.ServiceLifetime ?? ServiceLifetime.Transient))
        {
            services.TryAdd(item);
        }

        return services;
    }
}