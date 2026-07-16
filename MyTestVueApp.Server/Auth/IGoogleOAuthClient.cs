namespace MyTestVueApp.Server.Auth
{
    public interface IGoogleOAuthClient
    {
        string BuildAuthorizationUrl(string redirectUri, string state);
        Task<GoogleUserInfo> ExchangeCodeAsync(string code, string redirectUri);
    }
}
