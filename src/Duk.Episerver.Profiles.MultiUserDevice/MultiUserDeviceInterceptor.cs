using EPiServer.ServiceLocation;
using EPiServer.Tracking.Core;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Web;

namespace Duk.Episerver.Profiles.MultiUserDevice
{
    /// <summary>
    /// Tracking interceptor that overrides device ID cookie by email address hash if the event contains email address and comes from known users.
    /// It enables using the same device by different logged in users that have their email addresses.
    /// </summary>
    /// <seealso cref="EPiServer.Tracking.Core.ITrackingDataInterceptor" />
    [ServiceConfiguration(ServiceType = typeof(ITrackingDataInterceptor), Lifecycle = ServiceInstanceScope.Singleton)]
    public class MultiUserDeviceInterceptor : ITrackingDataInterceptor
    {
        public int SortOrder => 2000;

        const string AnonymousDeviceIdCookieName = "_madid_anonymous";
        const string DeviceIdCookieName = "_madid";
        const int DeviceCookiesExpirationInDays = 365;

        private readonly IHttpContextResolver _httpContextResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiUserDeviceInterceptor"/> class.
        /// </summary>
        /// <param name="httpContextResolver">The HTTP context resolver.</param>
        public MultiUserDeviceInterceptor(IHttpContextResolver httpContextResolver)
        {
            _httpContextResolver = httpContextResolver;
        }

        /// <summary>
        /// Intercepts the specified tracking data and overrides the device ID by email address hash for known visitors.
        /// </summary>
        /// <typeparam name="TPayload">The type of the payload.</typeparam>
        /// <param name="trackingData">The tracking data.</param>
        public void Intercept<TPayload>(TrackingData<TPayload> trackingData)
        {
            var httpContext = _httpContextResolver.GetContext();
            if (httpContext == null)
            {
                return;
            }

            var visitorEmail = trackingData.User?.Email;

            // If the visitor is anonymous (email is not known)
            if (string.IsNullOrWhiteSpace(visitorEmail))
            {
                // Get or create an anonymous device ID
                var anonymousDeviceId = GetCookie(AnonymousDeviceIdCookieName, httpContext);
                if (anonymousDeviceId == null)
                {
                    anonymousDeviceId = Guid.NewGuid().ToString();
                    SetCookie(AnonymousDeviceIdCookieName, anonymousDeviceId, httpContext);
                }

                // Get the device ID used for tracking
                var deviceId = GetCookie(DeviceIdCookieName, httpContext);
                // Check if device ID is anonymous device ID
                if (!string.Equals(anonymousDeviceId, deviceId, StringComparison.Ordinal))
                {
                    // Override device ID by anonymous ID if it is different
                    SetCookie(DeviceIdCookieName, anonymousDeviceId, httpContext);
                }
            }
            // If the visitor email is known
            else
            {
                // Create a hash from the email address to avoid storing email as is in the cookie
                var emailHash = CreateStringHash(visitorEmail);
                // Use email hash as device ID for known visitor and store in the cookie
                SetCookie(DeviceIdCookieName, emailHash, httpContext);
            }
        }

        private string CreateStringHash(string value)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashValue = sha256.ComputeHash(Encoding.UTF8.GetBytes(value));
                var builder = new StringBuilder();
                for (int i = 0; i < hashValue.Length; i++)
                {
                    builder.Append(hashValue[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private static string GetCookie(string cookieName, HttpContextBase httpContext)
        {
            return httpContext.Request.Cookies[cookieName]?.Value;
        }

        private void SetCookie(string cookieName, string cookieValue, HttpContextBase httpContext)
        {
            var cookie = new HttpCookie(cookieName)
            {
                Value = cookieValue,
                Expires = DateTime.UtcNow.AddDays(DeviceCookiesExpirationInDays)
            };

            UpdateCookieCollection(cookieName, cookie, httpContext.Request.Cookies);
            UpdateCookieCollection(cookieName, cookie, httpContext.Response.Cookies);
        }

        private static void UpdateCookieCollection(string cookieName, HttpCookie cookie, HttpCookieCollection cookies)
        {
            Monitor.Enter(cookies);
            try
            {
                if (cookies.AllKeys.Contains(cookieName))
                {
                    cookies.Remove(cookieName);
                }
                cookies.Add(cookie);
            }
            finally
            {
                Monitor.Exit(cookies);
            }
        }
    }
}
