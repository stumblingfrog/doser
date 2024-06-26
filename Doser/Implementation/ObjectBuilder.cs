﻿namespace Doser.Implementation;

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using Exceptions;

internal sealed class ObjectBuilder : IObjectResolver
{
    private readonly Type targetType;
    private readonly ResolverRepository resolvers;
    private Func<object>? creationFunction;
    
    public ObjectBuilder(Type targetType, ResolverRepository resolvers)
    {
        this.targetType = targetType;
        this.resolvers = resolvers;
    }

    public InstanceLifetime Lifetime => InstanceLifetime.Local;

    public object Resolve()
    {
        return this.creationFunction!();
    }

    public IObjectResolver Build()
    {
        this.creationFunction ??= this.GetCreationFunction();
        return this;
    }

    public IObjectResolver[] GetParentResolvers()
    {
        var constructor = this.GetConstructorInfo();
        return constructor.GetParameters()
            .Select<ParameterInfo, IObjectResolver>(item =>
            {
                var parameterType = item.ParameterType;

                var typeResolver = this.resolvers.GetResolver(parameterType);

                typeResolver.Build();
                var dependencyAttribute = Attribute.GetCustomAttribute(item, typeof(DependencyAttribute)) as DependencyAttribute;
                var resolver = dependencyAttribute == null
                    ? typeResolver.GetResolver()
                    : typeResolver.GetResolver(dependencyAttribute.Key);

                if (resolver != null)
                {
                    return resolver;
                }

                if (dependencyAttribute == null)
                {
                    throw new ResolveException(this.targetType);
                }
                throw new ResolveException(this.targetType, dependencyAttribute.Key);
            }).ToArray();
    }

    private Func<object> GetCreationFunction()
    {
        if (this.targetType.IsInterface)
        {
            throw new Exception($"Cannot construct interface {this.targetType.Name}");
        }

        if (this.targetType.IsAbstract)
        {
            throw new Exception($"Cannot construct abstract class {this.targetType.Name}");
        }

        var constructor = this.GetConstructorInfo();
        var parameters = constructor.GetParameters()
            .Select<ParameterInfo, Expression>(item =>
            {
                var parameterType = item.ParameterType;

                var typeResolver = this.resolvers.GetResolver(parameterType);
                
                typeResolver.Build();
                var dependencyAttribute = Attribute.GetCustomAttribute(item, typeof(DependencyAttribute)) as DependencyAttribute;
                var resolver = dependencyAttribute == null
                    ? typeResolver.GetResolver() 
                    : typeResolver.GetResolver(dependencyAttribute.Key);

                if (resolver == null)
                {
                    if (dependencyAttribute == null)
                    {
                        throw new ResolveException(this.targetType);
                    }
                    throw new ResolveException(this.targetType, dependencyAttribute.Key);
                }

                resolver.Build();

                if (resolver.Lifetime == InstanceLifetime.Global)
                {
                    return Expression.Constant(resolver.Resolve());
                }

                var resolverMethod = resolver.Resolve;
                var methodInfo = resolverMethod.GetMethodInfo();

                return Expression.Convert(Expression.Call(Expression.Constant(resolverMethod.Target), methodInfo), parameterType);
            });

        return (Func<object>)Expression.Lambda(Expression.New(constructor, parameters)).Compile();
    }
 
    private ConstructorInfo GetConstructorInfo()
    {
        var constructors = this.targetType.GetConstructors();
        return constructors.Length switch
        {
            0 => throw new Exception($"Type {this.targetType.FullName} has no constructors"),
            1 => constructors[0],
            _ => throw new Exception($"Type {this.targetType.FullName} has no suitable constructor")
        };
    }
}