using System;
using System.Collections.Generic;

namespace TWS.Infrastructure.DependencyInjection
{
    /// <summary>
    /// Simple dependency injection container
    /// </summary>
    public class ServiceContainer
    {
        private readonly Dictionary<Type, Func<ServiceContainer, object>> _factories = new Dictionary<Type, Func<ServiceContainer, object>>();
        private readonly Dictionary<Type, object> _singletons = new Dictionary<Type, object>();
        private readonly Dictionary<Type, Type> _transients = new Dictionary<Type, Type>();

        /// <summary>
        /// Registers a singleton service with a factory function
        /// </summary>
        public void RegisterSingleton<TInterface>(Func<ServiceContainer, TInterface> factory)
        {
            var interfaceType = typeof(TInterface);
            _factories[interfaceType] = c => factory(c);
        }

        /// <summary>
        /// Registers a singleton service with implementation type
        /// </summary>
        public void RegisterSingleton<TInterface, TImplementation>()
            where TImplementation : TInterface, new()
        {
            var interfaceType = typeof(TInterface);
            _factories[interfaceType] = c => new TImplementation();
        }

        /// <summary>
        /// Registers a transient service
        /// </summary>
        public void RegisterTransient<TInterface, TImplementation>()
            where TImplementation : TInterface, new()
        {
            _transients[typeof(TInterface)] = typeof(TImplementation);
        }

        /// <summary>
        /// Gets a service instance
        /// </summary>
        public T GetService<T>()
        {
            return (T)GetService(typeof(T));
        }

        /// <summary>
        /// Gets a service instance by type
        /// </summary>
        public object GetService(Type serviceType)
        {
            // Check if it's a singleton that's already created
            if (_singletons.ContainsKey(serviceType))
            {
                return _singletons[serviceType];
            }

            // Check if there's a factory for this type
            if (_factories.ContainsKey(serviceType))
            {
                var instance = _factories[serviceType](this);
                _singletons[serviceType] = instance;
                return instance;
            }

            // Check if it's a transient
            if (_transients.ContainsKey(serviceType))
            {
                return Activator.CreateInstance(_transients[serviceType]);
            }

            throw new InvalidOperationException($"Service of type {serviceType.Name} is not registered.");
        }

        /// <summary>
        /// Resolves a service instance (alias for GetService)
        /// </summary>
        public T Resolve<T>()
        {
            return GetService<T>();
        }

        /// <summary>
        /// Resolves a service instance by type (alias for GetService)
        /// </summary>
        public object Resolve(Type serviceType)
        {
            return GetService(serviceType);
        }

        /// <summary>
        /// Checks if a service is registered
        /// </summary>
        public bool IsRegistered<T>()
        {
            var type = typeof(T);
            return _singletons.ContainsKey(type) || _factories.ContainsKey(type) || _transients.ContainsKey(type);
        }

        /// <summary>
        /// Checks if a service type is registered
        /// </summary>
        public bool IsRegistered(Type serviceType)
        {
            return _singletons.ContainsKey(serviceType) || _factories.ContainsKey(serviceType) || _transients.ContainsKey(serviceType);
        }

        /// <summary>
        /// Clears all registered services
        /// </summary>
        public void Clear()
        {
            _factories.Clear();
            _singletons.Clear();
            _transients.Clear();
        }

        /// <summary>
        /// Disposes all disposable services
        /// </summary>
        public void Dispose()
        {
            foreach (var singleton in _singletons.Values)
            {
                if (singleton is IDisposable disposable)
                {
                    try
                    {
                        disposable.Dispose();
                    }
                    catch
                    {
                        // Suppress exceptions during disposal
                    }
                }
            }

            Clear();
        }
    }
}