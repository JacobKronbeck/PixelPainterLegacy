using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MyTestVueApp.Server.Configuration;
using MyTestVueApp.Server.Entities;
using MyTestVueApp.Server.Interfaces;
using MyTestVueApp.Server.ServiceImplementations;
using System.Security.Authentication;


namespace MyTestVueApp.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CommentLikeController : ControllerBase
    {
        private readonly IOptions<ApplicationConfiguration> AppConfig;
        private readonly ILogger<CommentLikeController> Logger;
        private readonly ICommentLikeService CommentLikeService;
        private readonly ICommentAccessService CommentService;
        private readonly ILoginService LoginService;

        public CommentLikeController(IOptions<ApplicationConfiguration> appConfig, ILogger<CommentLikeController> logger, ICommentLikeService commentLikeService, ILoginService loginService, ICommentAccessService commentService)
        {
            AppConfig = appConfig;
            Logger = logger;
            CommentService = commentService;
            CommentLikeService = commentLikeService;
            LoginService = loginService;
        }

        /// <summary>
        /// Marks the current user to like an artwork
        /// </summary>
        [HttpPost]
        [Route("InsertCommentLike")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> InsertCommentLike([FromQuery]int commentId)
        {
            try
            {
                // If the user is logged in
                if (Request.Cookies.TryGetValue("GoogleOAuth", out var userId))
                {
                    var artist = await LoginService.GetUserBySubId(userId);
                    if (artist != null)
                    {
                        // You can add additional checks here if needed
                        var rowsChanged = await CommentLikeService.InsertCommentLike(artist, commentId);
                        if (rowsChanged > 0) // If the like has sucessfully been inserted
                        {
                            return Ok();
                        }
                        else
                        {
                            throw new ArgumentException("Failed to insert like. User may have already liked this post.");
                        }
                    }
                    else
                    {
                        throw new AuthenticationException("User does not have an account.");
                    }
                }
                else
                {
                    throw new AuthenticationException("User is not logged in!");
                }
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
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
        /// Remove a like from the database
        /// </summary>

        [HttpDelete]
        [Route("RemoveCommentLike")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RemoveCommentLike([FromQuery] int commentId)
        {
            try
            {
                // If the user is logged in
                if (Request.Cookies.TryGetValue("GoogleOAuth", out var userId))
                {
                    var artist = await LoginService.GetUserBySubId(userId);
                    if (artist != null)
                    {
                        // You can add additional checks here if needed
                        var rowsChanged = await CommentLikeService.RemoveCommentLike(artist, commentId);
                        if (rowsChanged > 0) // If the like has sucessfully been removed
                        {
                            return Ok();
                        }
                        else
                        {
                            throw new ArgumentException("Did not remove target like. It may not have existed.");
                        }
                    }
                    else
                    {
                        throw new AuthenticationException("User does not have an account");
                    }
                }
                else
                {
                    throw new AuthenticationException("User is not logged in!");
                }
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
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
        /// Checks if artwork is liked by current user
        /// </summary>
        /// <returns>True if it is liked, false otherwise</returns>
        [HttpGet]
        [Route("IsCommentLiked")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> IsCommentLiked([FromQuery] int commentId)
        {
            try
            {
                if (Request.Cookies.TryGetValue("GoogleOAuth", out var userId))
                {
                    var artist = await LoginService.GetUserBySubId(userId);
                    if (artist != null)
                    {
                        var liked = await CommentLikeService.IsCommentLiked(artist, commentId);
                        return Ok(liked);
                    }
                    else
                    {
                        throw new AuthenticationException("User does not have an account");
                    }
                }
                else
                {
                    throw new AuthenticationException("User is not logged in!");
                }
            }
            catch (AuthenticationException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }
    }
}
