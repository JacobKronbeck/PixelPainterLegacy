using MyTestVueApp.Server.Auth;

namespace MyTestVueApp.Server.Tests
{
    public class FakeGoogleOAuthClient : IGoogleOAuthClient
    {
        public string BuildAuthorizationUrl(string redirectUri, string state)
        {
            return $"{redirectUri}?code=fake-code&state={Uri.EscapeDataString(state)}";
        }

        public Task<GoogleUserInfo> ExchangeCodeAsync(string code, string redirectUri)
        {
            return Task.FromResult(new GoogleUserInfo("google-sub-123456789", "artist@example.com"));
        }
    }
}
