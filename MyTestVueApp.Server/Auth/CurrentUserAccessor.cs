using MyTestVueApp.Server.Entities;
using MyTestVueApp.Server.Interfaces;
using System.Security.Claims;

namespace MyTestVueApp.Server.Auth
{
    public class CurrentUserAccessor : ICurrentUserAccessor
    {
        private readonly ILoginService _loginService;
        private readonly IWebHostEnvironment _environment;

        public CurrentUserAccessor(ILoginService loginService, IWebHostEnvironment environment)
        {
            _loginService = loginService;
            _environment = environment;
        }

        public async Task<Artist?> GetCurrentUserAsync(HttpContext httpContext)
        {
            var subId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(subId)
                && _environment.IsDevelopment()
                && IsLocalHost(httpContext.Request.Host.Host)
                && httpContext.Request.Cookies.TryGetValue(AuthCookieOptions.LegacyCookieName, out var legacySubId))
            {
                subId = legacySubId;
            }

            if (string.IsNullOrWhiteSpace(subId))
            {
                return null;
            }

            return await _loginService.GetUserBySubId(subId);
        }

        private static bool IsLocalHost(string host)
        {
            return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
                || string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase);
        }
    }
}
