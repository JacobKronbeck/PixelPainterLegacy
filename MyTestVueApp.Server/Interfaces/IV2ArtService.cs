using MyTestVueApp.Server.Contracts.V2;
using MyTestVueApp.Server.Entities;

namespace MyTestVueApp.Server.Interfaces
{
    public interface IV2ArtService
    {
        Task<IReadOnlyList<ArtDto>> GetArtByArtistAsync(int artistId, Artist? viewer);
        Task<IReadOnlyList<ArtDto>> GetLikedArtAsync(int artistId, Artist? viewer);
        Task<ArtDto> SaveArtAsync(CreateArtRequest request, Artist artist);
    }
}
