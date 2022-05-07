﻿namespace Doser.Implementation.Lifetime
{
    using System;
    using System.Collections.Concurrent;

    internal class Scope : IScope, IDisposable
    {
        private readonly ConcurrentDictionary<Guid, object> instances = new ();
        private readonly ThreadScopeService service;

        public Scope(ThreadScopeService service, IScope parent)
        {
            this.service = service;
            this.Parent = parent;
        }

        public IScope Parent { get; }

        public object Get(Guid key, Func<object> factory)
        {
            return this.instances.GetOrAdd(key, factory);
        }

        public void Dispose()
        {
            service.CloseScope(this);
        }

        public object GetTransparent(Guid key, Func<object> next)
        {
            if(Parent == null)
            {
                return this.Get(key, next);
            }
            return this.Get(key, () => this.Parent.Get(key, next));
        }
    }
}