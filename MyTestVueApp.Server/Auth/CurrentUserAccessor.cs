using MyTestVueApp.Server.Entities;
using MyTestVueApp.Server.Interfaces;

namespace MyTestVueApp.Server.Auth
{
    public class CurrentUserAccessor : ICurrentUserAccessor
    {
        private readonly ILoginService _loginService;

        public CurrentUserAccessor(ILoginService loginService)
        {
            _loginService = loginService;
        }

        public async Task<Artist?> GetCurrentUserAsync(HttpContext httpContext)
        {
            if (!httpContext.Request.Cookies.TryGetValue(AuthCookieOptions.CookieName, out var subId)
                || string.IsNullOrWhiteSpace(subId))
            {
                return null;
            }

            return await _loginService.GetUserBySubId(subId);
        }
    }
}
