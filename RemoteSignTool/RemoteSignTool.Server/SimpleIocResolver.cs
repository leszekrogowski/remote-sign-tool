using System;
using System.Collections.Generic;
using System.Web.Http.Dependencies;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Practices.ServiceLocation;

namespace RemoteSignTool.Server
{
    public class SimpleIocResolver : IDependencyResolver
    {
        public IDependencyScope BeginScope()
        {
            // SimpleIoc doesn't support scopes so return this object
            return this;
        }

        public object GetService(Type serviceType)
        {
            try
            {
                return SimpleIoc.Default.GetInstanceWithoutCaching(serviceType);
            }
            catch (ActivationException)
            {
                // Implemented according to: http://www.asp.net/web-api/overview/advanced/dependency-injection
                return null;
            }
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            try
            {
                return new List<object> { SimpleIoc.Default.GetInstanceWithoutCaching(serviceType) };
            }
            catch (ActivationException)
            {
                // Implemented according to: http://www.asp.net/web-api/overview/advanced/dependency-injection
                return new List<object>();
            }
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            // Do nothing
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        #endregion
    }
}
