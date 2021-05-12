using System.Web;

namespace Duk.Episerver.Profiles.MultiUserDevice
{
    /// <summary>
    /// Resolves the HTTP context object and allows injecting and mocking it.
    /// </summary>
    public interface IHttpContextResolver
    {
        /// <summary>
        /// Gets the HTTP context object.
        /// </summary>
        /// <returns></returns>
        HttpContextBase GetContext();
    }
}
