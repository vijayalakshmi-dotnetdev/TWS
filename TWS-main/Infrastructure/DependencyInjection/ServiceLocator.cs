using System;

namespace TWS.Infrastructure.DependencyInjection
{
    /// <summary>
    /// Static service locator for accessing the DI container
    /// </summary>


    // ============================================================================
    // SERVICE LOCATOR
    // ============================================================================
    public static class ServiceLocator
    {
        private static ServiceContainer _container;

        public static void Initialize(ServiceContainer container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        public static T GetService<T>()
        {
            if (_container == null)
            {
                throw new InvalidOperationException(
                    "ServiceLocator not initialized. Call Initialize() first.");
            }

            return _container.Resolve<T>();
        }
    }
}