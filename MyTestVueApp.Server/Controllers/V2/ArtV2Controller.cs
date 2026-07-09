using Microsoft.AspNetCore.Mvc;
using MyTestVueApp.Server.Auth;
using MyTestVueApp.Server.Contracts.V2;
using MyTestVueApp.Server.Interfaces;

namespace MyTestVueApp.Server.Controllers.V2
{
    [ApiController]
    [Route("api/v2/art")]
    public class ArtV2Controller : ControllerBase
    {
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly IV2ArtService _artService;

        public ArtV2Controller(ICurrentUserAccessor currentUserAccessor, IV2ArtService artService)
        {
            _currentUserAccessor = currentUserAccessor;
            _artService = artService;
        }

        [HttpPost]
        [ProducesResponseType(typeof(ArtDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> SaveArt([FromBody] CreateArtRequest request)
        {
            var user = await _currentUserAccessor.GetCurrentUserAsync(HttpContext);
            if (user == null)
            {
                return Unauthorized();
            }

            return Ok(await _artService.SaveArtAsync(request, user));
        }
    }
}
