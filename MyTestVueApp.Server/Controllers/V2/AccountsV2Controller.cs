using Microsoft.AspNetCore.Mvc;
using MyTestVueApp.Server.Auth;
using MyTestVueApp.Server.Contracts.V2;
using MyTestVueApp.Server.Interfaces;

namespace MyTestVueApp.Server.Controllers.V2
{
    [ApiController]
    [Route("api/v2/accounts")]
    public class AccountsV2Controller : ControllerBase
    {
        private readonly IV2AccountService _accountService;
        private readonly IV2ArtService _artService;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public AccountsV2Controller(
            IV2AccountService accountService,
            IV2ArtService artService,
            ICurrentUserAccessor currentUserAccessor)
        {
            _accountService = accountService;
            _artService = artService;
            _currentUserAccessor = currentUserAccessor;
        }

        [HttpGet("by-name/{username}")]
        [ProducesResponseType(typeof(AccountDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByName(string username)
        {
            var account = await _accountService.GetByNameAsync(username);
            return account == null ? NotFound() : Ok(account);
        }

        [HttpGet("{artistId:int}/art")]
        [ProducesResponseType(typeof(List<ArtDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetArt(int artistId)
        {
            var viewer = await _currentUserAccessor.GetCurrentUserAsync(HttpContext);
            return Ok(await _artService.GetArtByArtistAsync(artistId, viewer));
        }

        [HttpGet("{artistId:int}/liked-art")]
        [ProducesResponseType(typeof(List<ArtDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLikedArt(int artistId)
        {
            var viewer = await _currentUserAccessor.GetCurrentUserAsync(HttpContext);
            return Ok(await _artService.GetLikedArtAsync(artistId, viewer));
        }
    }
}
