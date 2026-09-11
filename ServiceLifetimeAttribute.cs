using System;
using Microsoft.Extensions.DependencyInjection;

namespace OdinCore;

public class ServiceLifetimeAttribute : Attribute
{
    private readonly ServiceLifetime _serviceLifetime;

    internal ServiceLifetime ServiceLifetime => _serviceLifetime;

    public ServiceLifetimeAttribute(ServiceLifetime serviceLifetime)
    {
        _serviceLifetime = serviceLifetime;
    }
}