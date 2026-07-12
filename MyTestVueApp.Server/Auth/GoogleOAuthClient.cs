using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Oauth2.v2;
using Google.Apis.Services;
using Google.Apis.Util;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using MyTestVueApp.Server.Configuration;

namespace MyTestVueApp.Server.Auth
{
    public class GoogleOAuthClient : IGoogleOAuthClient
    {
        private readonly IOptions<ApplicationConfiguration> _appConfig;

        public GoogleOAuthClient(IOptions<ApplicationConfiguration> appConfig)
        {
            _appConfig = appConfig;
        }

        public string BuildAuthorizationUrl(string redirectUri)
        {
            return QueryHelpers.AddQueryString("https://accounts.google.com/o/oauth2/v2/auth",
                new Dictionary<string, string?>
                {
                    ["client_id"] = _appConfig.Value.ClientId,
                    ["redirect_uri"] = redirectUri,
                    ["scope"] = "email profile",
                    ["response_type"] = "code",
                    ["prompt"] = "consent"
                });
        }

        public async Task<GoogleUserInfo> ExchangeCodeAsync(string code, string redirectUri)
        {
            var tokenResponse = new AuthorizationCodeTokenRequest
            {
                ClientId = _appConfig.Value.ClientId,
                ClientSecret = _appConfig.Value.ClientSecret,
                Code = code,
                GrantType = "authorization_code",
                RedirectUri = redirectUri
            };

            var result = await tokenResponse.ExecuteAsync(
                new HttpClient(),
                GoogleAuthConsts.TokenUrl,
                CancellationToken.None,
                SystemClock.Default);

            var credential = GoogleCredential.FromAccessToken(result.AccessToken);
            var oauth2Service = new Oauth2Service(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "PixelPainterLegacy"
            });

            var userInfo = await oauth2Service.Userinfo.Get().ExecuteAsync();
            return new GoogleUserInfo(userInfo.Id, userInfo.Email ?? string.Empty);
        }
    }
}
