using MyTestVueApp.Server.Database;
using MyTestVueApp.Server.Entities;
using MyTestVueApp.Server.Interfaces;

namespace MyTestVueApp.Server.ServiceImplementations
{
    public class LikeService : ILikeService
    {
        private readonly IPostgresDataAccess db;
        private readonly ILogger<LikeService> Logger;

        public LikeService(IPostgresDataAccess Db, ILogger<LikeService> logger)
        {
            db = Db;
            Logger = logger;
        }

        /// <summary>
        /// Insert's into the database what artwork an artist has liked
        /// </summary>
        /// <param name="artId">Id being lliked</param>
        /// <param name="artist">Id of the artist who liked the artwork</param>
        /// <returns>0 if invalid input, -1 if the input failed, and 1+ if it succeeded</returns>
        public async Task<int> InsertLike(int artId, Artist artist)
        {
            var count = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*)::int FROM Likes WHERE ArtistID = @ArtistId AND ArtID = @ArtId",
                command =>
                {
                    command.Parameters.AddWithValue("@ArtistId", artist.Id);
                    command.Parameters.AddWithValue("@ArtId", artId);
                });

            if (count > 0)
            {
                Console.WriteLine("This user has already liked this art piece!");
                return 0;
            }

            var rowsChanged = await db.ExecuteAsync(
                "INSERT INTO Likes (ArtistID, ArtID, Viewed) VALUES (@ArtistId, @ArtId, 0)",
                command =>
                {
                    command.Parameters.AddWithValue("@ArtistId", artist.Id);
                    command.Parameters.AddWithValue("@ArtId", artId);
                });

            return rowsChanged > 0 ? rowsChanged : -1;
        }

        /// <summary>
        /// Removes the like relation from the database
        /// </summary>
        /// <param name="artId">Artwork being unliked</param>
        /// <param name="artist">Artist who is unliking the artwork</param>
        /// <returns>0 if bad input, -1 if it fails, 1+ if it succeeds</returns>
        public async Task<int> RemoveLike(int artId, Artist artist)
        {
            var count = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*)::int FROM Likes WHERE ArtistID = @ArtistId AND ArtID = @ArtId",
                command =>
                {
                    command.Parameters.AddWithValue("@ArtistId", artist.Id);
                    command.Parameters.AddWithValue("@ArtId", artId);
                });

            if (count == 0)
            {
                Console.WriteLine("The like you are trying to delete doesnt exist!");
                return 0;
            }

            var rowsChanged = await db.ExecuteAsync(
                "DELETE FROM Likes WHERE ArtistID = @ArtistId AND ArtID = @ArtId",
                command =>
                {
                    command.Parameters.AddWithValue("@ArtistId", artist.Id);
                    command.Parameters.AddWithValue("@ArtId", artId);
                });

            return rowsChanged > 0 ? rowsChanged : -1;
        }

        /// <summary>
        /// Checks to see if an artwork is liked by the user
        /// </summary>
        /// <param name="artId">Id of the artwork being checked</param>
        /// <param name="artist">Id of the user who would've liked the post</param>
        /// <returns>Returns true if it is liked by the given artist, false otherwise</returns>
        public async Task<bool> IsLiked(int artId, Artist artist)
        {
            var count = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*)::int FROM Likes WHERE ArtistId = @ArtistId AND ArtID = @ArtID",
                command =>
                {
                    command.Parameters.AddWithValue("@ArtistId", artist.Id);
                    command.Parameters.AddWithValue("@ArtID", artId);
                });

            return count > 0;
        }

        /// <summary>
        /// Gets all likes an artwork has
        /// </summary>
        /// <param name="artworkId">Id of the artwork being referenced</param>
        /// <returns>A list of Like objects</returns>
        public async Task<IEnumerable<Like>> GetLikesByArtwork(int artworkId)
        {
            //Need to Append Created On to query when added to database
            string likedQuery =
                $@"
                        SELECT Artist.Name, Art.Title, Likes.ArtId, Likes.ArtistId, Likes.Viewed
                        FROM Likes
                        LEFT JOIN Art ON Art.ID = Likes.ArtID
                        LEFT JOIN Artist on Artist.Id = Likes.ArtistId
                        WHERE Likes.ArtId = @artworkId";

            return await db.QueryAsync(
                likedQuery,
                command => command.Parameters.AddWithValue("@artworkId", artworkId),
                reader => new Like
                {
                    Artist = reader.GetString(0),
                    Artwork = reader.GetString(1),
                    ArtId = reader.GetInt32(2),
                    ArtistId = reader.GetInt32(3),
                    Viewed = reader.GetInt32(4) == 1,
                    LikedOn = new DateTime()
                });
        }

        /// <summary>
        /// Gets the Like object that belong to the artist and artwork referenced
        /// </summary>
        /// <param name="artId">Id of the art being checked</param>
        /// <param name="artistId">Id of the artist who would've made the like</param>
        /// <returns>A Like object if found, null otherwise</returns>
        public async Task<Like> GetLikeByIds(int artId, int artistId)
        {
            //Need to Append Created On to query when added to database
            string likedQuery =
                $@"
                          SELECT Artist.Name, Art.Title, Likes.ArtId, Likes.ArtistId, Likes.Viewed
                          FROM Likes
                          LEFT JOIN Art ON Art.ID = Likes.ArtID
                          LEFT JOIN Artist ON Likes.ArtistId = Artist.Id
                          WHERE Likes.ArtId = @art and Likes.ArtistId = @artist
                          ";

            var likes = await db.QueryAsync(
                likedQuery,
                command =>
                {
                    command.Parameters.AddWithValue("@artist", artistId);
                    command.Parameters.AddWithValue("@art", artId);
                },
                reader => new Like
                {
                    Artist = reader.GetString(0),
                    Artwork = reader.GetString(1),
                    ArtId = reader.GetInt32(2),
                    ArtistId = reader.GetInt32(3),
                    Viewed = reader.GetInt32(4) == 1,
                    LikedOn = new DateTime()
                });

            if (likes.Count > 0)
            {
                return likes[0];
            }

            throw new ArgumentException("No like data in the datbase matches values art id: " + artId + " and artist id: " + artistId);
        }
    }
}
