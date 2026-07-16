using MyTestVueApp.Server.Entities;
using MyTestVueApp.Server.Interfaces;
using System.Security.Claims;

namespace MyTestVueApp.Server.Auth
{
    public class CurrentUserAccessor : ICurrentUserAccessor
    {
        private readonly IV2AccountService _accountService;
        private readonly IWebHostEnvironment _environment;

        public CurrentUserAccessor(IV2AccountService accountService, IWebHostEnvironment environment)
        {
            _accountService = accountService;
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

            var account = await _accountService.GetBySubIdAsync(subId);
            if (account == null)
            {
                return null;
            }

            return new Artist
            {
                Id = account.Id,
                SubId = subId,
                Name = account.Name,
                IsAdmin = account.IsAdmin,
                PrivateProfile = account.PrivateProfile,
                CreationDate = account.CreationDate,
                Email = account.Email,
                NotificationsEnabled = account.NotificationsEnabled
            };
        }

        private static bool IsLocalHost(string host)
        {
            return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
                || string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase);
        }
    }
}
