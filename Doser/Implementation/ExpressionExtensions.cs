﻿namespace Doser.Implementation;

using System;
using System.Linq.Expressions;
using System.Reflection;

internal static class ExpressionExtensions
{
    public static Expression CreateResolveExpression(this IObjectResolver resolver, Type resultType)
    {
        var resolverMethod = resolver.Resolve;
        var methodInfo = resolverMethod.GetMethodInfo();
        
        // static / instance method call
        return methodInfo.DeclaringType == null 
            ? Expression.Convert(Expression.Invoke(Expression.Constant(methodInfo)), resultType) 
            : Expression.Convert(Expression.Call(Expression.Constant(resolverMethod.Target), methodInfo), resultType);
    }
}