using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MyTestVueApp.Server.Auth;
using MyTestVueApp.Server.Configuration;
using MyTestVueApp.Server.Contracts.V2;
using MyTestVueApp.Server.Interfaces;

namespace MyTestVueApp.Server.Controllers.V2
{
    [ApiController]
    [Route("api/v2/auth")]
    public class AuthV2Controller : ControllerBase
    {
        private readonly IGoogleOAuthClient _googleOAuthClient;
        private readonly IV2AccountService _accountService;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly IOptions<ApplicationConfiguration> _appConfig;

        public AuthV2Controller(
            IGoogleOAuthClient googleOAuthClient,
            IV2AccountService accountService,
            ICurrentUserAccessor currentUserAccessor,
            IOptions<ApplicationConfiguration> appConfig)
        {
            _googleOAuthClient = googleOAuthClient;
            _accountService = accountService;
            _currentUserAccessor = currentUserAccessor;
            _appConfig = appConfig;
        }

        [HttpGet("login")]
        public IActionResult Login()
        {
            return Redirect(_googleOAuthClient.BuildAuthorizationUrl(GetOAuthRedirectUri()));
        }

        [HttpGet("callback")]
        [ProducesResponseType(StatusCodes.Status302Found)]
        public async Task<IActionResult> Callback([FromQuery] string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return BadRequest("Missing OAuth code.");
            }

            var googleUser = await _googleOAuthClient.ExchangeCodeAsync(code, GetOAuthRedirectUri());
            var account = await _accountService.GetOrCreateFromGoogleAsync(googleUser);

            Response.Cookies.Append(
                AuthCookieOptions.CookieName,
                googleUser.SubjectId,
                AuthCookieOptions.Create());

            return Redirect(BuildFrontendAccountUrl(account.Name));
        }

        [HttpGet("me")]
        [ProducesResponseType(typeof(AuthSessionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Me()
        {
            var user = await _currentUserAccessor.GetCurrentUserAsync(HttpContext);
            if (user == null)
            {
                return Unauthorized();
            }

            return Ok(new AuthSessionDto(true, _accountService.ToDto(user)));
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete(AuthCookieOptions.CookieName, AuthCookieOptions.Create(DateTimeOffset.UtcNow.AddDays(-1)));
            return Ok();
        }

        private string GetOAuthRedirectUri()
        {
            if (!string.IsNullOrWhiteSpace(_appConfig.Value.OAuthRedirectUrl))
            {
                return _appConfig.Value.OAuthRedirectUrl.Trim();
            }

            var scheme = (Request.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? Request.Scheme).Split(',')[0].Trim();
            var host = (Request.Headers["X-Forwarded-Host"].FirstOrDefault() ?? Request.Host.Value).Split(',')[0].Trim();
            if (!host.StartsWith("localhost", StringComparison.OrdinalIgnoreCase)
                && !host.StartsWith("127.0.0.1", StringComparison.OrdinalIgnoreCase))
            {
                scheme = "https";
            }

            return $"{scheme}://{host}/api/v2/auth/callback";
        }

        private string BuildFrontendAccountUrl(string username)
        {
            var baseUrl = !string.IsNullOrWhiteSpace(_appConfig.Value.PostLoginRedirectUrl)
                ? _appConfig.Value.PostLoginRedirectUrl
                : _appConfig.Value.RedirectUrl;

            baseUrl = (baseUrl ?? "/").TrimEnd('/');
            return $"{baseUrl}/accountpage/{Uri.EscapeDataString(username)}#created_art";
        }
    }
}
