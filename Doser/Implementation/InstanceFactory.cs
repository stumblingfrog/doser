﻿namespace Doser.Implementation;

using System;

internal class InstanceFactory : IObjectResolver
{
    private readonly Func<object> instanceFactory;

    public InstanceFactory(Func<object> instanceFactory, InstanceLifetime lifetime)
    {
        this.instanceFactory = instanceFactory;
        this.Lifetime = lifetime;
    }

    public InstanceLifetime Lifetime { get; } 

    public Func<object> GetResolver()
    {
        return instanceFactory;
    }

    public void Build() { }
}