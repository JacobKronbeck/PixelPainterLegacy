using System.Security.Claims;

namespace Microsoft.AspNetCore.Http
{
    public static class HttpContextAuthExtensions
    {
        public static bool TryGetCurrentUserSubId(this HttpContext context, out string subId)
        {
            subId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(subId) && context.User.Identity?.IsAuthenticated == true)
            {
                return true;
            }

            // Transitional support for existing local integration tests and local development data.
            // Production never trusts the old raw subject-id cookie.
            var environment = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
            if (environment.IsDevelopment()
                && IsLocalHost(context.Request.Host.Host)
                && context.Request.Cookies.TryGetValue(
                    MyTestVueApp.Server.Auth.AuthCookieOptions.LegacyCookieName,
                    out var legacySubId)
                && !string.IsNullOrWhiteSpace(legacySubId))
            {
                subId = legacySubId;
                return true;
            }

            return false;
        }

        private static bool IsLocalHost(string host)
        {
            return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
                || string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase);
        }
    }
}
