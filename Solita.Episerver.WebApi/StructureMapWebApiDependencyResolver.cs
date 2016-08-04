using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http.Dependencies;
using StructureMap;

namespace Solita.Episerver.WebApi
{
    /// <summary>
    /// Web API DependencyResolver for StructureMap
    /// </summary>
    public class StructureMapWebApiDependencyResolver : IDependencyResolver
    {
        private readonly IContainer _container;

        public StructureMapWebApiDependencyResolver(IContainer container)
        {
            _container = container;
        }
        
        public IDependencyScope BeginScope()
        {
            return this; 
        }
        
        public void Dispose()
        {
            
        }
        
        public object GetService(Type serviceType)
        {
            if (serviceType.IsInterface || serviceType.IsAbstract)
            {
                return GetInterfaceService(serviceType);
            }
            return GetConcreteService(serviceType);
        }

        private object GetConcreteService(Type serviceType)
        {
            try
            {
                // Can't use TryGetInstance here because it won’t create concrete types
                return _container.GetInstance(serviceType);
            }
            catch (StructureMapException)
            {
                return null;
            }
        }

        private object GetInterfaceService(Type serviceType)
        {
            return _container.TryGetInstance(serviceType);
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return _container.GetAllInstances(serviceType).Cast<object>();
        }
    }
}