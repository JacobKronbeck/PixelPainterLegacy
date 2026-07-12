namespace MyTestVueApp.Server.Auth
{
    public interface IGoogleOAuthClient
    {
        string BuildAuthorizationUrl(string redirectUri);
        Task<GoogleUserInfo> ExchangeCodeAsync(string code, string redirectUri);
    }
}
