using Microsoft.AspNetCore.Mvc;
using MyTestVueApp.Server.Interfaces;
using Microsoft.Extensions.Logging;
using MyTestVueApp.Server.ServiceImplementations;
using MyTestVueApp.Server.Entities;
using System.Security.Authentication;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using MyTestVueApp.Server.Auth;

namespace MyTestVueApp.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LoginController : ControllerBase
    {
        private readonly ILogger<ArtAccessController> Logger;
        private readonly ILoginService LoginService;

        public LoginController(ILogger<ArtAccessController> logger, ILoginService loginService)
        {
            Logger = logger;
            LoginService = loginService;
        }

        [HttpGet]
        [Route("Login")]
        public IActionResult Login()
        {
            return Redirect("/api/v2/auth/login");
        }

        [HttpGet]
        [Route("LoginRedirect")]
        public IActionResult RedirectLogin(string code, string scope, string authuser, string prompt)
        {
            return StatusCode(
                StatusCodes.Status410Gone,
                "The legacy OAuth callback is retired. Start login at /api/v2/auth/login.");
        }

        [HttpPost]
        [Route("Logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            Response.Cookies.Delete(AuthCookieOptions.LegacyCookieName);
            return Ok();
        }

        /// <summary>
        /// Checks if a user is logged in
        /// </summary>
        /// <returns>True if they are logged in, false otherwise</returns>
        [HttpGet]
        [Route("IsLoggedIn")]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> IsLoggedIn()
        {
            try
            {
                if (HttpContext.TryGetCurrentUserSubId(out var userId))
                {
                    var artist = await LoginService.GetUserBySubId(userId);
                    return Ok(artist != null);
                }
                return Ok(false);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Login status check failed");
                return Ok(false);
            }
        }
        /// <summary>
        /// Get all artists
        /// </summary>
        /// <returns>A list of artists</returns>
        [HttpGet]
        [Route("GetAllArtists")]
        [ProducesResponseType(typeof(List<Artist>), 200)]
        public async Task<IActionResult> GetAllArtists()
        {
            try
            {
                var artist = await LoginService.GetAllArtists();
                return Ok(artist);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Current user lookup failed");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, "Unable to retrieve current user.");
            }
        }
        /// <summary>
        /// Get the current user's information
        /// </summary>
        /// <returns>A artist object</returns>
        [HttpGet]
        [Route("GetCurrentUser")]
        [ProducesResponseType(typeof(Artist), 200)]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                if (HttpContext.TryGetCurrentUserSubId(out var userId))
                {
                    var artist = await LoginService.GetUserBySubId(userId);
                    if (artist == null) {
                        throw new InvalidDataException("Artist is null.");
                    }
                    return Ok(artist);
                }
                throw new AuthenticationException("User is not logged in.");
            }
            catch (AuthenticationException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (InvalidDataException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }
        /// <summary>
        /// This Function changes the PrivateProfile value, making it the inverse of the initial value
        /// </summary>
        /// <param name="artistId"> ID of the artist</param>
        /// <returns>A true when changing from public to private or false when changing from private to public</returns>
        [HttpPut]
        [Route("privateSwitchChange")]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> PrivateSwitchChange([FromBody, BindRequired]int artistId)
        {

            try
            {
                if (HttpContext.TryGetCurrentUserSubId(out var userId))
                {
                    var artist = await LoginService.GetUserBySubId(userId);
                    if (artist.IsAdmin || artist.Id == artistId)
                    {
                        var status = await LoginService.PrivateSwitchChange(artistId);
                        return Ok(status);
                    }
                    throw new InvalidDataException("User is not an admin or the orignal artist.");
                }
                throw new AuthenticationException("User is not logged in.");
            }
            catch (AuthenticationException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (InvalidDataException ex) { 
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }
        /// <summary>
        /// This function grabs the artist by intaking a name to see if they are there
        /// </summary>
        /// <param name="name">Name of the artist</param>
        /// <returns>An artist</returns>
        [HttpGet]
        [Route("GetArtistByName")]
        [ProducesResponseType(typeof(Artist), 200)]
        public async Task<IActionResult> GetArtistByName([FromQuery] string name)
        {
            try
            {
                var artist = await LoginService.GetArtistByName(name);
                return Ok(artist);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        /// <summary>
        /// Check to see if a user is an admin
        /// </summary>
        /// <returns>True if the user is an admin, false otherwise</returns>
        [HttpGet]
        [Route("GetIsAdmin")]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> GetIsAdmin()
        {
            try
            {
                // If the user is logged in
                if (HttpContext.TryGetCurrentUserSubId(out var userId))
                {
                    var artist = await LoginService.GetUserBySubId(userId);
                    if(artist == null) { return Ok(false); }
                    if (artist.IsAdmin)
                    {
                        return Ok(true);
                    }
                    else { return Ok(false); }
                }
                else
                {
                    return Ok(false);
                }
            } 
            catch(ArgumentException ex)
            {
                Logger.LogWarning(ex, "Admin check failed");
                return Ok(false);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Admin check failed");
                return Ok(false);
            }
        }
        /// <summary>
        /// Updates the current user's username
        /// </summary>
        /// <param name="newUsername">New Username</param>
        /// <returns>True if successful, false otherwise</returns>
        [HttpPut]
        [Route("UpdateUsername")]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> UpdateUsername([FromQuery] string newUsername)
        {
            try
            {
                if (HttpContext.TryGetCurrentUserSubId(out var subId))
                {
                    var success = await LoginService.UpdateUsername(newUsername, subId);
                    return Ok(success);
                }
                else
                {
                    throw new AuthenticationException("User is not logged in");
                }
            }
            catch (AuthenticationException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }
        /// <summary>
        /// Removes the current user from the database
        /// </summary>
        /// <param name="id">Id of the user to remove</param>
        [HttpDelete]
        [Route("DeleteArtist")]
        public async Task<IActionResult> DeleteArtist([FromQuery] int id)
        {
            try
            {
                // If the user is logged in
                if (HttpContext.TryGetCurrentUserSubId(out var userId))
                {
                    var artist = await LoginService.GetUserBySubId(userId);
                    if(artist.Id == id)
                    {
                        LoginService.DeleteArtist(artist.Id);
                        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        Response.Cookies.Delete(AuthCookieOptions.LegacyCookieName);
                        return Ok();
                    }
                    else if (artist.IsAdmin)
                    {
                        LoginService.DeleteArtist(id);
                        return Ok();
                    }
                    else {
                        throw new InvalidCredentialException("User is not allowed to preform this action");
                    }
                }
                else
                {
                    throw new AuthenticationException("User is not logged in");
                }
            }
            catch (InvalidCredentialException ex)
            {
                return Forbid(ex.Message);
            }
            catch (AuthenticationException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }
        /// <summary>
        /// Sets admin privileges for a user (admin only).
        /// </summary>
        /// <param name="artistId">Target artist id.</param>
        /// <param name="isAdmin">True to grant admin, false to revoke.</param>
        [HttpPost]
        [Route("SetAdmin")]
        public async Task<IActionResult> SetAdmin([FromQuery] int artistId, [FromQuery] bool isAdmin)
        {
            try
            {
                // Authenticate current user via cookie
                if (!HttpContext.TryGetCurrentUserSubId(out var subId))
                    throw new AuthenticationException("User is not logged in!");

                var currentUser = await LoginService.GetUserBySubId(subId);
                if (currentUser == null)
                    throw new AuthenticationException("User does not have an account.");

                // Authorize admin
                if (!currentUser.IsAdmin)
                    throw new AuthenticationException("User does not have permission to set admin.");

                var ok = await LoginService.UpdateIsAdmin(artistId, isAdmin);
                if (!ok) return NotFound($"Artist with id {artistId} not found.");

                return Ok();
            }
            catch (AuthenticationException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

    }
}
