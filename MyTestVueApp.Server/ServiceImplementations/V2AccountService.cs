using Microsoft.Extensions.Options;
using MyTestVueApp.Server.Auth;
using MyTestVueApp.Server.Configuration;
using MyTestVueApp.Server.Contracts.V2;
using MyTestVueApp.Server.Entities;
using MyTestVueApp.Server.Interfaces;
using Npgsql;

namespace MyTestVueApp.Server.ServiceImplementations
{
    public class V2AccountService : IV2AccountService
    {
        private readonly IOptions<ApplicationConfiguration> _appConfig;

        private static readonly string[] Adjectives =
        {
            "Happy", "Bright", "Quick", "Calm", "Pixel", "Color", "Soft", "Sharp",
            "Tiny", "Bold", "Lucky", "Neon", "Quiet", "Sunny", "Fresh", "Magic"
        };

        private static readonly string[] Nouns =
        {
            "Brush", "Canvas", "Grid", "Palette", "Sprite", "Frame", "Layer", "Sketch",
            "Pixel", "Mosaic", "Glow", "Doodle", "Tile", "Painter", "Dot", "Ink"
        };

        public V2AccountService(IOptions<ApplicationConfiguration> appConfig)
        {
            _appConfig = appConfig;
        }

        public AccountDto ToDto(Artist artist)
        {
            return new AccountDto(
                artist.Id,
                artist.Name ?? string.Empty,
                artist.IsAdmin,
                artist.PrivateProfile,
                artist.CreationDate,
                artist.Email ?? string.Empty,
                artist.NotificationsEnabled);
        }

        public async Task<AccountDto?> GetBySubIdAsync(string subId)
        {
            await using var connection = new SqlConnection(_appConfig.Value.ConnectionString);
            await connection.OpenAsync();
            await using var command = new SqlCommand(@"
                SELECT id, subid, name, isadmin, privateprofile, creationdate, email, notificationsenabled
                FROM artist
                WHERE subid = @SubId
                LIMIT 1;", connection);
            command.Parameters.AddWithValue("@SubId", subId);

            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            return ToDto(MapArtist(reader));
        }

        public async Task<AccountDto?> GetByNameAsync(string name)
        {
            await using var connection = new SqlConnection(_appConfig.Value.ConnectionString);
            await connection.OpenAsync();
            await using var command = new SqlCommand(@"
                SELECT id, subid, name, isadmin, privateprofile, creationdate, email, notificationsenabled
                FROM artist
                WHERE lower(name) = lower(@Name)
                LIMIT 1;", connection);
            command.Parameters.AddWithValue("@Name", name);

            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            return ToDto(MapArtist(reader));
        }

        public async Task<AccountDto> GetOrCreateFromGoogleAsync(GoogleUserInfo googleUser)
        {
            await using var connection = new SqlConnection(_appConfig.Value.ConnectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            var existing = await FindBySubIdAsync(connection, transaction, googleUser.SubjectId);
            if (existing != null)
            {
                await transaction.CommitAsync();
                return ToDto(existing);
            }

            for (var attempt = 0; attempt < 8; attempt++)
            {
                var username = GenerateUsername();
                if (await UsernameExistsAsync(connection, transaction, username))
                {
                    continue;
                }

                await using var insert = new SqlCommand(@"
                    INSERT INTO artist (subid, name, email, isadmin, creationdate, privateprofile, notificationsenabled)
                    VALUES (@SubId, @Name, @Email, false, @CreationDate, false, 63)
                    RETURNING id, subid, name, isadmin, privateprofile, creationdate, email, notificationsenabled;",
                    connection,
                    transaction);
                insert.Parameters.AddWithValue("@SubId", googleUser.SubjectId);
                insert.Parameters.AddWithValue("@Name", username);
                insert.Parameters.AddWithValue("@Email", googleUser.Email);
                insert.Parameters.AddWithValue("@CreationDate", DateTime.UtcNow);

                try
                {
                    Artist? created = null;
                    await using (var reader = await insert.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            created = MapArtist(reader);
                        }
                    }

                    if (created != null)
                    {
                        await transaction.CommitAsync();
                        return ToDto(created);
                    }
                }
                catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
                {
                    if (attempt == 7)
                    {
                        throw;
                    }
                }
            }

            throw new InvalidOperationException("Unable to generate a unique username.");
        }

        private static async Task<Artist?> FindBySubIdAsync(SqlConnection connection, SqlTransaction transaction, string subId)
        {
            await using var command = new SqlCommand(@"
                SELECT id, subid, name, isadmin, privateprofile, creationdate, email, notificationsenabled
                FROM artist
                WHERE subid = @SubId
                LIMIT 1;", connection, transaction);
            command.Parameters.AddWithValue("@SubId", subId);

            await using var reader = await command.ExecuteReaderAsync();
            return await reader.ReadAsync() ? MapArtist(reader) : null;
        }

        private static async Task<bool> UsernameExistsAsync(SqlConnection connection, SqlTransaction transaction, string username)
        {
            await using var command = new SqlCommand(
                "SELECT COUNT(1)::int FROM artist WHERE lower(name) = lower(@Name);",
                connection,
                transaction);
            command.Parameters.AddWithValue("@Name", username);
            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        private static Artist MapArtist(SqlDataReader reader)
        {
            return new Artist
            {
                Id = reader.GetInt32(0),
                SubId = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                Name = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                IsAdmin = !reader.IsDBNull(3) && reader.GetBoolean(3),
                PrivateProfile = !reader.IsDBNull(4) && reader.GetBoolean(4),
                CreationDate = reader.IsDBNull(5) ? DateTime.UtcNow : reader.GetDateTime(5),
                Email = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                NotificationsEnabled = reader.IsDBNull(7) ? 63 : reader.GetInt32(7)
            };
        }

        private static string GenerateUsername()
        {
            var suffix = Random.Shared.Next(1000, 9999).ToString();
            var name = Adjectives[Random.Shared.Next(Adjectives.Length)] + Nouns[Random.Shared.Next(Nouns.Length)];
            var maxBaseLength = Math.Max(4, 20 - suffix.Length);
            return (name.Length > maxBaseLength ? name[..maxBaseLength] : name) + suffix;
        }
    }
}
