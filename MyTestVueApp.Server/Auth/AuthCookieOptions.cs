namespace MyTestVueApp.Server.Auth
{
    public static class AuthCookieOptions
    {
        public const string CookieName = "PixelPainterAuth";
        public const string LegacyCookieName = "GoogleOAuth";
        public const string OAuthStateCookieName = "PixelPainterOAuthState";

        public static CookieOptions Create(DateTimeOffset? expires = null)
        {
            return new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                Expires = expires ?? DateTimeOffset.UtcNow.AddDays(14)
            };
        }

        public static CookieOptions CreateOAuthState()
        {
            return new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                MaxAge = TimeSpan.FromMinutes(10)
            };
        }
    }
}
