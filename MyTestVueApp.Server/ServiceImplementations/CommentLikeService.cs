using MyTestVueApp.Server.Database;
using MyTestVueApp.Server.Entities;
using MyTestVueApp.Server.Interfaces;

namespace MyTestVueApp.Server.ServiceImplementations
{
    public class CommentLikeService : ICommentLikeService
    {
        private readonly IPostgresDataAccess db;
        private readonly ILogger<CommentLikeService> Logger;

        public CommentLikeService(IPostgresDataAccess Db, ILogger<CommentLikeService> logger)
        {
            db = Db;
            Logger = logger;
        }

        /// <summary>
        /// Insert's into the database what comment an artist has liked
        /// </summary>
        /// <param name="artist">Id of the artist who liked the comment</param>
        /// <param name="commentId">Id of the comment being liked</param>
        /// <returns>0 if invalid input, -1 if the input failed, and 1+ if it succeeded</returns>
        public async Task<int> InsertCommentLike(Artist artist, int commentId)
        {
            var count = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*)::int FROM CommentLikes WHERE ArtistID = @ArtistId AND CommentID = @CommentId",
                command =>
                {
                    command.Parameters.AddWithValue("@ArtistId", artist.Id);
                    command.Parameters.AddWithValue("@CommentId", commentId);
                });

            if (count > 0)
            {
                Console.WriteLine("This user has already liked this comment!");
                return 0;
            }

            var rowsChanged = await db.ExecuteAsync(
                "INSERT INTO CommentLikes (ArtistID, CommentID, Viewed) VALUES (@ArtistId, @CommentId, 0)",
                command =>
                {
                    command.Parameters.AddWithValue("@ArtistId", artist.Id);
                    command.Parameters.AddWithValue("@CommentId", commentId);
                });

            return rowsChanged > 0 ? rowsChanged : -1;
        }

        /// <summary>
        /// Removes the like relation from the database
        /// </summary>
        /// <param name="artist">Artist who is unliking the comment</param>
        /// <param name="commentId">Id of the comment being unliked</param>
        /// <returns>0 if bad input, -1 if it fails, 1+ if it succeeds</returns>
        public async Task<int> RemoveCommentLike(Artist artist, int commentId)
        {
            var count = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*)::int FROM CommentLikes WHERE ArtistID = @ArtistId AND CommentID = @CommentId",
                command =>
                {
                    command.Parameters.AddWithValue("@ArtistId", artist.Id);
                    command.Parameters.AddWithValue("@CommentId", commentId);
                });

            if (count == 0)
            {
                Console.WriteLine("The like you are trying to delete doesnt exist!");
                return 0;
            }

            var rowsChanged = await db.ExecuteAsync(
                "DELETE FROM CommentLikes WHERE ArtistID = @ArtistId AND CommentID = @CommentId",
                command =>
                {
                    command.Parameters.AddWithValue("@ArtistId", artist.Id);
                    command.Parameters.AddWithValue("@CommentId", commentId);
                });

            return rowsChanged > 0 ? rowsChanged : -1;
        }

        /// <summary>
        /// Checks to see if an comment is liked by the user
        /// </summary>
        /// <param name="artist">Id of the user who would've liked the comment</param>
        /// <param name="commentId">Id of the comment being checked</param>
        /// <returns>Returns true if it is liked by the given artist, false otherwise</returns>
        public async Task<bool> IsCommentLiked(Artist artist, int commentId)
        {
            var count = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*)::int FROM CommentLikes WHERE ArtistId = @ArtistId AND CommentID = @CommentId",
                command =>
                {
                    command.Parameters.AddWithValue("@CommentID", commentId);
                    command.Parameters.AddWithValue("@ArtistID", artist.Id);
                });

            return count > 0;
        }

        /// <summary>
        /// Gets all likes a comment has
        /// </summary>
        /// <param name="commentId">Id of the comment being referenced</param>
        /// <returns>A list of Like objects</returns>
        public async Task<IEnumerable<CommentLike>> GetLikesByComment(int commentId)
        {
            //Need to Append Created On to query when added to database
            string commentlikedQuery =
                $@"
                        SELECT Artist.Name, CommentLikes.CommentId, CommentLikes.ArtistId, CommentLikes.Viewed
                        FROM CommentLikes
                        LEFT JOIN Comment ON Comment.ID = CommentLikes.CommentID
                        LEFT JOIN Artist on Artist.Id = CommentLikes.ArtistId
                        WHERE CommentLikes.CommentId = @commentId";

            return await db.QueryAsync(
                commentlikedQuery,
                command => command.Parameters.AddWithValue("@commentId", commentId),
                reader => new CommentLike
                {
                    Artist = reader.GetString(0),
                    CommentId = reader.GetInt32(1),
                    ArtistId = reader.GetInt32(2),
                    Viewed = reader.GetInt32(3) == 1,
                });
        }
    }
}
