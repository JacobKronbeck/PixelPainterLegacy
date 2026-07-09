namespace MyTestVueApp.Server.Auth
{
    public static class AuthCookieOptions
    {
        public const string CookieName = "GoogleOAuth";

        public static CookieOptions Create(DateTimeOffset? expires = null)
        {
            return new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = expires ?? DateTimeOffset.UtcNow.AddDays(14)
            };
        }
    }
}
