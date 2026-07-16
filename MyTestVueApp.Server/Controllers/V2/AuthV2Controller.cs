using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using MyTestVueApp.Server.Auth;
using MyTestVueApp.Server.Configuration;
using MyTestVueApp.Server.Contracts.V2;
using MyTestVueApp.Server.Interfaces;
using System.Security.Cryptography;
using System.Text;

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
        private readonly IWebHostEnvironment _environment;

        public AuthV2Controller(
            IGoogleOAuthClient googleOAuthClient,
            IV2AccountService accountService,
            ICurrentUserAccessor currentUserAccessor,
            IOptions<ApplicationConfiguration> appConfig,
            IWebHostEnvironment environment)
        {
            _googleOAuthClient = googleOAuthClient;
            _accountService = accountService;
            _currentUserAccessor = currentUserAccessor;
            _appConfig = appConfig;
            _environment = environment;
        }

        /// <summary>
        /// Signs in with a real local database account for Swagger development testing.
        /// </summary>
        /// <remarks>
        /// This endpoint is available only in the Development environment through a localhost request.
        /// It creates the account on first use, reuses it on later calls, and sets the same cookie used
        /// by Google OAuth. After it succeeds, Swagger automatically sends the cookie to protected endpoints.
        /// </remarks>
        [HttpPost("local-login")]
        [ProducesResponseType(typeof(AuthSessionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> LocalLogin([FromBody] LocalLoginRequest request)
        {
            if (!_environment.IsDevelopment() || !IsLocalRequest())
            {
                return NotFound();
            }

            var account = await SignInLocalAccountAsync(request.Email);

            return Ok(new AuthSessionDto(true, account));
        }

        /// <summary>
        /// Starts Google OAuth, or signs into the local Swagger account when Google is not configured.
        /// </summary>
        [HttpGet("login")]
        [ProducesResponseType(StatusCodes.Status302Found)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> Login()
        {
            if (!HasGoogleOAuthConfiguration())
            {
                if (!_environment.IsDevelopment() || !IsLocalRequest())
                {
                    return Problem(
                        title: "Google OAuth is not configured.",
                        statusCode: StatusCodes.Status503ServiceUnavailable);
                }

                await SignInLocalAccountAsync("swagger@example.com");
                return Redirect("/swagger/index.html");
            }

            var state = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
            Response.Cookies.Append(
                AuthCookieOptions.OAuthStateCookieName,
                state,
                AuthCookieOptions.CreateOAuthState());

            return Redirect(_googleOAuthClient.BuildAuthorizationUrl(GetOAuthRedirectUri(), state));
        }

        [HttpGet("callback")]
        [ProducesResponseType(StatusCodes.Status302Found)]
        public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string state)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return BadRequest("Missing OAuth code.");
            }

            if (!TryValidateOAuthState(state))
            {
                return BadRequest("Invalid or expired OAuth state.");
            }

            Response.Cookies.Delete(
                AuthCookieOptions.OAuthStateCookieName,
                AuthCookieOptions.Create(DateTimeOffset.UtcNow.AddDays(-1)));

            var googleUser = await _googleOAuthClient.ExchangeCodeAsync(code, GetOAuthRedirectUri());
            var account = await _accountService.GetOrCreateFromGoogleAsync(googleUser);
            await SignInAccountAsync(account, googleUser.SubjectId);

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
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            Response.Cookies.Delete(AuthCookieOptions.LegacyCookieName);
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

        private bool IsLocalRequest()
        {
            var host = Request.Host.Host;
            return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
                || string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase);
        }

        private bool HasGoogleOAuthConfiguration()
        {
            return !string.IsNullOrWhiteSpace(_appConfig.Value.ClientId)
                && !string.IsNullOrWhiteSpace(_appConfig.Value.ClientSecret);
        }

        private async Task<AccountDto> SignInLocalAccountAsync(string requestedEmail)
        {
            var email = requestedEmail.Trim().ToLowerInvariant();
            var localUser = new GoogleUserInfo(CreateLocalSubjectId(email), email);
            var account = await _accountService.GetOrCreateFromGoogleAsync(localUser);
            await SignInAccountAsync(account, localUser.SubjectId);

            return account;
        }

        private async Task SignInAccountAsync(AccountDto account, string subjectId)
        {
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                AuthPrincipalFactory.Create(account, subjectId),
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    AllowRefresh = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14)
                });
        }

        private bool TryValidateOAuthState(string state)
        {
            if (string.IsNullOrWhiteSpace(state)
                || !Request.Cookies.TryGetValue(AuthCookieOptions.OAuthStateCookieName, out var expectedState)
                || string.IsNullOrWhiteSpace(expectedState))
            {
                return false;
            }

            var actualBytes = Encoding.UTF8.GetBytes(state);
            var expectedBytes = Encoding.UTF8.GetBytes(expectedState);
            return actualBytes.Length == expectedBytes.Length
                && CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
        }

        private static string CreateLocalSubjectId(string email)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(email));
            return "local-" + Convert.ToHexString(hash)[..15].ToLowerInvariant();
        }
    }
}
