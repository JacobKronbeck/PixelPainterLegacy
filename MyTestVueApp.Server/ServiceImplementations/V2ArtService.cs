using Microsoft.Extensions.Options;
using MyTestVueApp.Server.Configuration;
using MyTestVueApp.Server.Contracts.V2;
using MyTestVueApp.Server.Entities;
using MyTestVueApp.Server.Interfaces;

namespace MyTestVueApp.Server.ServiceImplementations
{
    public class V2ArtService : IV2ArtService
    {
        private readonly IOptions<ApplicationConfiguration> _appConfig;
        private readonly ITagService _tagService;
        private readonly IArtAccessService _legacyArtService;

        public V2ArtService(
            IOptions<ApplicationConfiguration> appConfig,
            ITagService tagService,
            IArtAccessService legacyArtService)
        {
            _appConfig = appConfig;
            _tagService = tagService;
            _legacyArtService = legacyArtService;
        }

        public async Task<IReadOnlyList<ArtDto>> GetArtByArtistAsync(int artistId, Artist? viewer)
        {
            var includePrivate = viewer != null && (viewer.Id == artistId || viewer.IsAdmin);
            return await QueryArtAsync(@"
                SELECT
                    a.id,
                    COALESCE(a.title, '') AS title,
                    COALESCE(a.width, 0) AS width,
                    COALESCE(a.height, 0) AS height,
                    COALESCE(a.encode, '') AS encode,
                    COALESCE(a.creationdate, now()) AS creationdate,
                    COALESCE(a.ispublic, false) AS ispublic,
                    COALESCE(a.isgif, false) AS isgif,
                    COALESCE(a.gifid, 0) AS gifid,
                    COALESCE(a.gifframenum, 0) AS gifframenum,
                    COALESCE(a.pointid, 0) AS pointid,
                    COUNT(DISTINCT l.artistid)::int AS likes,
                    COUNT(DISTINCT d.artistid)::int AS dislikes,
                    COUNT(DISTINCT c.id)::int AS comments
                FROM art a
                INNER JOIN contributingartists ca ON ca.artid = a.id
                LEFT JOIN likes l ON l.artid = a.id
                LEFT JOIN dislikes d ON d.artid = a.id
                LEFT JOIN comment c ON c.artid = a.id
                WHERE ca.artistid = @ArtistId
                    AND COALESCE(a.gifframenum, 0) <= 1
                    AND (@IncludePrivate OR COALESCE(a.ispublic, false))
                GROUP BY a.id, a.title, a.width, a.height, a.encode, a.creationdate,
                    a.ispublic, a.isgif, a.gifid, a.gifframenum, a.pointid
                ORDER BY COALESCE(a.creationdate, now()) DESC;",
                command =>
                {
                    command.Parameters.AddWithValue("@ArtistId", artistId);
                    command.Parameters.AddWithValue("@IncludePrivate", includePrivate);
                },
                viewer);
        }

        public async Task<IReadOnlyList<ArtDto>> GetLikedArtAsync(int artistId, Artist? viewer)
        {
            var includePrivate = viewer != null && (viewer.Id == artistId || viewer.IsAdmin);
            return await QueryArtAsync(@"
                SELECT
                    a.id,
                    COALESCE(a.title, '') AS title,
                    COALESCE(a.width, 0) AS width,
                    COALESCE(a.height, 0) AS height,
                    COALESCE(a.encode, '') AS encode,
                    COALESCE(a.creationdate, now()) AS creationdate,
                    COALESCE(a.ispublic, false) AS ispublic,
                    COALESCE(a.isgif, false) AS isgif,
                    COALESCE(a.gifid, 0) AS gifid,
                    COALESCE(a.gifframenum, 0) AS gifframenum,
                    COALESCE(a.pointid, 0) AS pointid,
                    COUNT(DISTINCT all_likes.artistid)::int AS likes,
                    COUNT(DISTINCT d.artistid)::int AS dislikes,
                    COUNT(DISTINCT c.id)::int AS comments
                FROM likes liked
                INNER JOIN art a ON a.id = liked.artid
                LEFT JOIN likes all_likes ON all_likes.artid = a.id
                LEFT JOIN dislikes d ON d.artid = a.id
                LEFT JOIN comment c ON c.artid = a.id
                WHERE liked.artistid = @ArtistId
                    AND COALESCE(a.gifframenum, 0) <= 1
                    AND (@IncludePrivate OR COALESCE(a.ispublic, false))
                GROUP BY a.id, a.title, a.width, a.height, a.encode, a.creationdate,
                    a.ispublic, a.isgif, a.gifid, a.gifframenum, a.pointid
                ORDER BY COALESCE(a.creationdate, now()) DESC;",
                command =>
                {
                    command.Parameters.AddWithValue("@ArtistId", artistId);
                    command.Parameters.AddWithValue("@IncludePrivate", includePrivate);
                },
                viewer);
        }

        public async Task<ArtDto> SaveArtAsync(CreateArtRequest request, Artist artist)
        {
            var art = new Art
            {
                Id = request.Id,
                Title = request.Title ?? string.Empty,
                IsPublic = request.IsPublic,
                PixelGrid = request.PixelGrid,
                Tags = request.Tags ?? new List<Tag>(),
                ArtistId = new[] { artist.Id },
                ArtistName = new[] { artist.Name }
            };

            var saved = request.Id == 0
                ? await _legacyArtService.SaveNewArt(artist, art)
                : await _legacyArtService.UpdateArt(art);

            saved.Tags = (await _tagService.GetTagsForArt(saved.Id)).ToList();
            saved.SetArtists((await _legacyArtService.GetArtistsByArtId(saved.Id)).ToList());
            return MapArt(saved, artist);
        }

        private async Task<IReadOnlyList<ArtDto>> QueryArtAsync(string sql, Action<SqlCommand> configure, Artist? viewer)
        {
            var artRows = new List<Art>();
            await using var connection = new SqlConnection(_appConfig.Value.ConnectionString);
            await connection.OpenAsync();
            await using var command = new SqlCommand(sql, connection);
            configure(command);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                artRows.Add(MapArtRow(reader));
            }

            var results = new List<ArtDto>();
            foreach (var art in artRows)
            {
                art.SetArtists((await _legacyArtService.GetArtistsByArtId(art.Id)).ToList());
                art.Tags = (await _tagService.GetTagsForArt(art.Id)).ToList();
                results.Add(MapArt(art, viewer));
            }

            return results;
        }

        private Art MapArtRow(SqlDataReader reader)
        {
            var art = new Art
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                PixelGrid = new PixelGrid
                {
                    Width = reader.GetInt32(2),
                    Height = reader.GetInt32(3),
                    BackgroundColor = "FFFFFF",
                    EncodedGrid = reader.GetString(4)
                },
                CreationDate = reader.GetDateTime(5),
                IsPublic = reader.GetBoolean(6),
                IsGif = reader.GetBoolean(7),
                GifID = reader.GetInt32(8),
                GifFrameNum = reader.GetInt32(9),
                PointId = reader.GetInt32(10),
                NumLikes = reader.GetInt32(11),
                NumDislikes = reader.GetInt32(12),
                NumComments = reader.GetInt32(13),
                PointTitle = string.Empty,
                ArtspaceTitle = string.Empty,
                Tags = new List<Tag>()
            };

            return art;
        }

        private static ArtDto MapArt(Art art, Artist? viewer)
        {
            var artistIds = art.ArtistId ?? Array.Empty<int>();
            return new ArtDto(
                art.Id,
                artistIds,
                art.ArtistName ?? Array.Empty<string>(),
                art.Title ?? string.Empty,
                art.IsPublic,
                art.CreationDate,
                new PixelGridDto(
                    art.PixelGrid?.Width ?? 0,
                    art.PixelGrid?.Height ?? 0,
                    art.PixelGrid?.BackgroundColor ?? "FFFFFF",
                    art.PixelGrid?.EncodedGrid ?? string.Empty),
                (art.Tags ?? new List<Tag>())
                    .Select(t => new TagDto(t.Id, t.Name ?? string.Empty, t.CreationDate))
                    .ToArray(),
                art.IsGif,
                art.GifID,
                art.GifFrameNum,
                art.GifFps,
                art.NumLikes,
                art.NumDislikes,
                art.NumComments,
                art.PointId,
                art.PointTitle ?? string.Empty,
                art.ArtspaceId,
                art.ArtspaceTitle ?? string.Empty,
                viewer != null && artistIds.Contains(viewer.Id));
        }
    }
}
