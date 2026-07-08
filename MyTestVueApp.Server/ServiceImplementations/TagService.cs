using MyTestVueApp.Server.Database;
using MyTestVueApp.Server.Entities;
using MyTestVueApp.Server.Interfaces;
using Microsoft.Extensions.Logging;

namespace MyTestVueApp.Server.ServiceImplementations
{
    public class TagService : ITagService
    {
        private readonly IPostgresDataAccess _db;
        private readonly ILogger<TagService> _logger;

        public TagService(IPostgresDataAccess db, ILogger<TagService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<IEnumerable<Tag>> GetAllTags()
        {
            return await _db.QueryAsync(
                "SELECT Id, Name, CreationDate FROM Tag",
                null,
                reader => new Tag
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    CreationDate = reader.GetDateTime(2)
                });
        }

        public async Task<Tag> CreateTag(Tag tag)
        {
            tag.Id = await _db.ExecuteScalarAsync<int>(
                "INSERT INTO Tag (Name, CreationDate) VALUES (@Name, @CreationDate) RETURNING Id",
                command =>
                {
                    command.Parameters.AddWithValue("@Name", tag.Name);
                    command.Parameters.AddWithValue("@CreationDate", tag.CreationDate == default ? DateTime.UtcNow : tag.CreationDate);
                });

            return tag;
        }

        public async Task<bool> AssignTagsToArt(int artId, List<int> tagIds)
        {
            var exists = await _db.ExecuteScalarAsync<int>(
                "SELECT COUNT(1)::int FROM Art WHERE Id = @ArtId",
                command => command.Parameters.AddWithValue("@ArtId", artId)) > 0;
            if (!exists)
            {
                throw new ArgumentException($"Art with Id {artId} does not exist.");
            }

            return await _db.ExecuteInTransactionAsync(async (conn, tran) =>
            {
                using var deleteCmd = new SqlCommand(
                    "DELETE FROM ArtTags WHERE ArtId = @ArtId", conn, tran);
                deleteCmd.Parameters.AddWithValue("@ArtId", artId);
                await deleteCmd.ExecuteNonQueryAsync();

                foreach (var tagId in tagIds.Distinct())
                {
                    using var insertCmd = new SqlCommand(
                        "INSERT INTO ArtTags (ArtId, TagId, CreationDate) VALUES (@ArtId, @TagId, @CreationDate)", conn, tran);
                    insertCmd.Parameters.AddWithValue("@ArtId", artId);
                    insertCmd.Parameters.AddWithValue("@TagId", tagId);
                    insertCmd.Parameters.AddWithValue("@CreationDate", DateTime.UtcNow);
                    await insertCmd.ExecuteNonQueryAsync();
                }

                return true;
            });
        }

        public async Task<IEnumerable<Tag>> GetTagsForArt(int artId)
        {
            return await _db.QueryAsync(
                @"SELECT t.Id, t.Name, t.CreationDate
                  FROM Tag t
                  INNER JOIN ArtTags at ON t.Id = at.TagId
                  WHERE at.ArtId = @ArtId",
                command => command.Parameters.AddWithValue("@ArtId", artId),
                reader => new Tag
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    CreationDate = reader.GetDateTime(2)
                });
        }

        public async Task<bool> RemoveTagFromArt(int artId, int tagId, Artist artist)
        {
            var rows = await _db.ExecuteAsync(
                "DELETE FROM ArtTags WHERE ArtId = @ArtId AND TagId = @TagId",
                command =>
                {
                    command.Parameters.AddWithValue("@ArtId", artId);
                    command.Parameters.AddWithValue("@TagId", tagId);
                });

            return rows > 0;
        }

        public async Task<bool> DeleteTag(int tagId)
        {
            return await _db.ExecuteInTransactionAsync(async (conn, tran) =>
            {
                using var existsCmd = new SqlCommand("SELECT COUNT(1)::int FROM Tag WHERE Id = @Id", conn, tran);
                existsCmd.Parameters.AddWithValue("@Id", tagId);
                var exists = (int)await existsCmd.ExecuteScalarAsync() > 0;
                if (!exists)
                {
                    return false;
                }

                using var delLinks = new SqlCommand("DELETE FROM ArtTags WHERE TagId = @Id", conn, tran);
                delLinks.Parameters.AddWithValue("@Id", tagId);
                await delLinks.ExecuteNonQueryAsync();

                using var delTag = new SqlCommand("DELETE FROM Tag WHERE Id = @Id", conn, tran);
                delTag.Parameters.AddWithValue("@Id", tagId);
                var affected = await delTag.ExecuteNonQueryAsync();

                return affected > 0;
            });
        }
    }
}
