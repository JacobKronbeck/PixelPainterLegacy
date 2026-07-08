using MyTestVueApp.Server.Database;
using MyTestVueApp.Server.Entities;
using MyTestVueApp.Server.Interfaces;

namespace MyTestVueApp.Server.ServiceImplementations
{
    public class CommentDislikeService : ICommentDislikeService
    {
        private readonly IPostgresDataAccess db;
        private readonly ILogger<CommentDislikeService> Logger;

        public CommentDislikeService(IPostgresDataAccess Db, ILogger<CommentDislikeService> logger)
        {
            db = Db;
            Logger = logger;
        }

        /// <summary>
        /// Insert's into the database what comment an artist has disliked
        /// </summary>
        /// <param name="artist">Id of the artist who disliked the comment</param>
        /// <param name="commentId">Id of the comment being disliked</param>
        /// <returns>0 if invalid input, -1 if the input failed, and 1+ if it succeeded</returns>
        public async Task<int> InsertCommentDislike(Artist artist, int commentId)
        {
            var count = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*)::int FROM CommentDislikes WHERE ArtistID = @ArtistId AND CommentID = @CommentId",
                command =>
                {
                    command.Parameters.AddWithValue("@ArtistId", artist.Id);
                    command.Parameters.AddWithValue("@CommentId", commentId);
                });

            if (count > 0)
            {
                Console.WriteLine("This user has already disliked this comment!");
                return 0;
            }

            var rowsChanged = await db.ExecuteAsync(
                "INSERT INTO CommentDislikes (ArtistID, CommentID, Viewed) VALUES (@ArtistId, @CommentId, 0)",
                command =>
                {
                    command.Parameters.AddWithValue("@ArtistId", artist.Id);
                    command.Parameters.AddWithValue("@CommentId", commentId);
                });

            return rowsChanged > 0 ? rowsChanged : -1;
        }

        /// <summary>
        /// Removes the dislike relation from the database
        /// </summary>
        /// <param name="artist">Artist who is undisliking the comment</param>
        /// <param name="commentId">Id of the comment being undisliked</param>
        /// <returns>0 if bad input, -1 if it fails, 1+ if it succeeds</returns>
        public async Task<int> RemoveCommentDislike(Artist artist, int commentId)
        {
            var count = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*)::int FROM CommentDislikes WHERE ArtistID = @ArtistId AND CommentID = @CommentId",
                command =>
                {
                    command.Parameters.AddWithValue("@ArtistId", artist.Id);
                    command.Parameters.AddWithValue("@CommentId", commentId);
                });

            if (count == 0)
            {
                Console.WriteLine("The dislike you are trying to delete doesnt exist!");
                return 0;
            }

            var rowsChanged = await db.ExecuteAsync(
                "DELETE FROM CommentDislikes WHERE ArtistID = @ArtistId AND CommentID = @CommentId",
                command =>
                {
                    command.Parameters.AddWithValue("@ArtistId", artist.Id);
                    command.Parameters.AddWithValue("@CommentId", commentId);
                });

            return rowsChanged > 0 ? rowsChanged : -1;
        }

        /// <summary>
        /// Checks to see if an comment is disliked by the user
        /// </summary>
        /// <param name="artist">Id of the user who would've disliked the comment</param>
        /// <param name="commentId">Id of the comment being checked</param>
        /// <returns>Returns true if it is disliked by the given artist, false otherwise</returns>
        public async Task<bool> IsCommentDisliked(Artist artist, int commentId)
        {
            var count = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*)::int FROM CommentDislikes WHERE ArtistId = @ArtistId AND CommentID = @CommentId",
                command =>
                {
                    command.Parameters.AddWithValue("@CommentID", commentId);
                    command.Parameters.AddWithValue("@ArtistID", artist.Id);
                });

            return count > 0;
        }

        /// <summary>
        /// Gets all dislikes a comment has
        /// </summary>
        /// <param name="commentId">Id of the comment being referenced</param>
        /// <returns>A list of CommentDislike objects</returns>
        public async Task<IEnumerable<CommentDislike>> GetDislikesByComment(int commentId)
        {
            //Need to Append Created On to query when added to database
            string commentdislikedQuery =
                $@"
                        SELECT Artist.Name, CommentDislikes.CommentId, CommentDislikes.ArtistId, CommentDislikes.Viewed
                        FROM CommentDislikes
                        LEFT JOIN Comment ON Comment.ID = CommentDislikes.CommentID
                        LEFT JOIN Artist on Artist.Id = CommentDislikes.ArtistId
                        WHERE CommentDislikes.CommentId = @commentId";

            return await db.QueryAsync(
                commentdislikedQuery,
                command => command.Parameters.AddWithValue("@commentId", commentId),
                reader => new CommentDislike
                {
                    Artist = reader.GetString(0),
                    CommentId = reader.GetInt32(1),
                    ArtistId = reader.GetInt32(2),
                    Viewed = reader.GetInt32(3) == 1,
                });
        }
    }
}
