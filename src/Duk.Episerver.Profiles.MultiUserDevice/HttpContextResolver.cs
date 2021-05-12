using EPiServer.ServiceLocation;
using System.Web;

namespace Duk.Episerver.Profiles.MultiUserDevice
{
    /// <summary>
    /// Resolves the HTTP current context.
    /// </summary>
    /// <seealso cref="Duk.Episerver.Profiles.MultiUserDevice.IHttpContextResolver" />
    [ServiceConfiguration(ServiceType = typeof(IHttpContextResolver), Lifecycle = ServiceInstanceScope.Singleton)]
    public class HttpContextResolver : IHttpContextResolver
    {
        /// <summary>
        /// Gets the current HTTP context if it is available.
        /// </summary>
        /// <returns></returns>
        public HttpContextBase GetContext()
        {
            var httpContext = HttpContext.Current;
            return httpContext == null ? null : new HttpContextWrapper(httpContext);
        }
    }
}
