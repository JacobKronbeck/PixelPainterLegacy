using MyTestVueApp.Server.Database;
using MyTestVueApp.Server.Entities;
using MyTestVueApp.Server.Interfaces;

namespace MyTestVueApp.Server.ServiceImplementations
{
    public class DislikeService : IDislikeService
    {
        private readonly IPostgresDataAccess db;
        private readonly ILogger<DislikeService> Logger;

        public DislikeService(IPostgresDataAccess Db, ILogger<DislikeService> logger)
        {
            db = Db;
            Logger = logger;
        }

        /// <summary>
        /// Insert's into the database what artwork an artist has disliked
        /// </summary>
        /// <param name="artId">Id being disliked</param>
        /// <param name="artist">Id of the artist who disliked the artwork</param>
        /// <returns>0 if invalid input, -1 if the input failed, and 1+ if it succeeded</returns>
        public async Task<int> InsertDislike(int artId, Artist artist)
        {
            var count = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*)::int FROM Dislikes WHERE ArtistID = @ArtistId AND ArtID = @ArtId",
                command =>
                {
                    command.Parameters.AddWithValue("@ArtistId", artist.Id);
                    command.Parameters.AddWithValue("@ArtId", artId);
                });

            if (count > 0)
            {
                Console.WriteLine("This user has already disliked this art piece!");
                return 0;
            }

            var rowsChanged = await db.ExecuteAsync(
                "INSERT INTO Dislikes (ArtistID, ArtID, Viewed) VALUES (@ArtistId, @ArtId, 0)",
                command =>
                {
                    command.Parameters.AddWithValue("@ArtistId", artist.Id);
                    command.Parameters.AddWithValue("@ArtId", artId);
                });

            return rowsChanged > 0 ? rowsChanged : -1;
        }

        /// <summary>
        /// Removes the dislike relation from the database
        /// </summary>
        /// <param name="artId">Artwork being undisliked</param>
        /// <param name="artist">Artist who is undisliking the artwork</param>
        /// <returns>0 if bad input, -1 if it fails, 1+ if it succeeds</returns>
        public async Task<int> RemoveDislike(int artId, Artist artist)
        {
            var count = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*)::int FROM Dislikes WHERE ArtistID = @ArtistId AND ArtID = @ArtId",
                command =>
                {
                    command.Parameters.AddWithValue("@ArtistId", artist.Id);
                    command.Parameters.AddWithValue("@ArtId", artId);
                });

            if (count == 0)
            {
                Console.WriteLine("The dislike you are trying to delete doesnt exist!");
                return 0;
            }

            var rowsChanged = await db.ExecuteAsync(
                "DELETE FROM Dislikes WHERE ArtistID = @ArtistId AND ArtID = @ArtId",
                command =>
                {
                    command.Parameters.AddWithValue("@ArtistId", artist.Id);
                    command.Parameters.AddWithValue("@ArtId", artId);
                });

            return rowsChanged > 0 ? rowsChanged : -1;
        }

        /// <summary>
        /// Checks to see if an artwork is disliked by the user
        /// </summary>
        /// <param name="artId">Id of the artwork being checked</param>
        /// <param name="artist">Id of the user who would've disliked the post</param>
        /// <returns>Returns true if it is disliked by the given artist, false otherwise</returns>
        public async Task<bool> IsDisliked(int artId, Artist artist)
        {
            var count = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*)::int FROM Dislikes WHERE ArtistId = @ArtistId AND ArtID = @ArtID",
                command =>
                {
                    command.Parameters.AddWithValue("@ArtistId", artist.Id);
                    command.Parameters.AddWithValue("@ArtID", artId);
                });

            return count > 0;
        }

        /// <summary>
        /// Gets all dislikes an artwork has
        /// </summary>
        /// <param name="artworkId">Id of the artwork being referenced</param>
        /// <returns>A list of Dislike objects</returns>
        public async Task<IEnumerable<Dislike>> GetDislikesByArtwork(int artworkId)
        {
            //Need to Append Created On to query when added to database
            string dislikedQuery =
                $@"
                        SELECT Artist.Name, Art.Title, Dislikes.ArtId, Dislikes.ArtistId, Dislikes.Viewed
                        FROM Dislikes
                        LEFT JOIN Art ON Art.ID = Dislikes.ArtID
                        LEFT JOIN Artist on Artist.Id = Dislikes.ArtistId
                        WHERE Dislikes.ArtId = @artworkId";

            return await db.QueryAsync(
                dislikedQuery,
                command => command.Parameters.AddWithValue("@artworkId", artworkId),
                reader => new Dislike
                {
                    Artist = reader.GetString(0),
                    Artwork = reader.GetString(1),
                    ArtId = reader.GetInt32(2),
                    ArtistId = reader.GetInt32(3),
                    Viewed = reader.GetInt32(4) == 1,
                    DislikedOn = new DateTime()
                });
        }

        /// <summary>
        /// Gets the dislike object that belong to the artist and artwork referenced
        /// </summary>
        /// <param name="artId">Id of the art being checked</param>
        /// <param name="artistId">Id of the artist who would've made the dislike</param>
        /// <returns>A dislike object if found, null otherwise</returns>
        public async Task<Dislike> GetDislikeByIds(int artId, int artistId)
        {
            //Need to Append Created On to query when added to database
            string dislikedQuery =
                $@"
                          SELECT Artist.Name, Art.Title, Dislikes.ArtId, Dislikes.ArtistId, Dislikes.Viewed
                          FROM Dislikes
                          LEFT JOIN Art ON Art.ID = Dislikes.ArtID
                          LEFT JOIN Artist ON Dislikes.ArtistId = Artist.Id
                          WHERE Dislikes.ArtId = @art and Dislikes.ArtistId = @artist
                          ";

            var dislikes = await db.QueryAsync(
                dislikedQuery,
                command =>
                {
                    command.Parameters.AddWithValue("@artist", artistId);
                    command.Parameters.AddWithValue("@art", artId);
                },
                reader => new Dislike
                {
                    Artist = reader.GetString(0),
                    Artwork = reader.GetString(1),
                    ArtId = reader.GetInt32(2),
                    ArtistId = reader.GetInt32(3),
                    Viewed = reader.GetInt32(4) == 1,
                    DislikedOn = new DateTime()
                });

            if (dislikes.Count > 0)
            {
                return dislikes[0];
            }

            throw new ArgumentException("No dislike data in the datbase matches values art id: " + artId + " and artist id: " + artistId);
        }
    }
}
