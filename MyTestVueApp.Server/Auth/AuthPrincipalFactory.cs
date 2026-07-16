using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using MyTestVueApp.Server.Contracts.V2;

namespace MyTestVueApp.Server.Auth
{
    public static class AuthPrincipalFactory
    {
        public static ClaimsPrincipal Create(AccountDto account, string subjectId)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, subjectId),
                new(ClaimTypes.Name, account.Name),
                new(ClaimTypes.Email, account.Email),
                new("artist_id", account.Id.ToString())
            };

            if (account.IsAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }

            return new ClaimsPrincipal(new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme));
        }
    }
}
