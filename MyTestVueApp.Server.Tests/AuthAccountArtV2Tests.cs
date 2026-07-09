using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using MyTestVueApp.Server.Contracts.V2;
using Npgsql;
using Xunit;

namespace MyTestVueApp.Server.Tests
{
    public class AuthAccountArtV2Tests : IClassFixture<PixelPainterApiFactory>
    {
        private readonly PixelPainterApiFactory _factory;

        public AuthAccountArtV2Tests(PixelPainterApiFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Me_ReturnsUnauthorized_WhenAnonymous()
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/api/v2/auth/me");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Callback_CreatesAccount_SetsCookie_AndRedirectsToAccountPage()
        {
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            var response = await client.GetAsync("/api/v2/auth/callback?code=test-code");

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/accountpage/", response.Headers.Location!.ToString());
            Assert.EndsWith("#created_art", response.Headers.Location!.ToString());
            Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
            Assert.Contains(cookies, cookie => cookie.StartsWith("GoogleOAuth=", StringComparison.Ordinal));

            var me = await client.GetFromJsonAsync<AuthSessionDto>("/api/v2/auth/me");
            Assert.NotNull(me);
            Assert.True(me!.IsAuthenticated);
            Assert.Equal("artist@example.com", me.User.Email);
        }

        [Fact]
        public async Task Callback_ReusesExistingGoogleAccount()
        {
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            await client.GetAsync("/api/v2/auth/callback?code=test-code");
            await client.GetAsync("/api/v2/auth/callback?code=test-code");

            await using var connection = new NpgsqlConnection(_factory.ConnectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand("SELECT COUNT(1)::int FROM artist WHERE subid = 'google-sub-123456789';", connection);

            Assert.Equal(1, (int)(await command.ExecuteScalarAsync())!);
        }

        [Fact]
        public async Task AccountLookup_ReturnsNotFound_ForMissingUsername()
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/api/v2/accounts/by-name/no-such-user");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task AccountArt_RespectsVisibility_AndReturnsEmptyLikedArt()
        {
            var ownerId = await SeedArtistAsync("owner-sub", "OwnerUser", "owner@example.com");
            await SeedArtAsync(ownerId, "Public Art", true);
            await SeedArtAsync(ownerId, "Private Art", false);

            var anonymousClient = _factory.CreateClient();
            var anonymousArt = await anonymousClient.GetFromJsonAsync<List<ArtDto>>($"/api/v2/accounts/{ownerId}/art");
            Assert.Single(anonymousArt!);
            Assert.Equal("Public Art", anonymousArt![0].Title);

            var liked = await anonymousClient.GetFromJsonAsync<List<ArtDto>>($"/api/v2/accounts/{ownerId}/liked-art");
            Assert.Empty(liked!);

            var ownerClient = _factory.CreateClient();
            ownerClient.DefaultRequestHeaders.Add("Cookie", "GoogleOAuth=owner-sub");
            var ownerArt = await ownerClient.GetFromJsonAsync<List<ArtDto>>($"/api/v2/accounts/{ownerId}/art");
            Assert.Equal(2, ownerArt!.Count);
        }

        [Fact]
        public async Task LikedArt_RespectsVisibility()
        {
            var ownerId = await SeedArtistAsync("likes-owner-sub", "LikesOwner", "likes-owner@example.com");
            var creatorId = await SeedArtistAsync("likes-creator-sub", "LikesCreator", "likes-creator@example.com");
            var publicArtId = await SeedArtAsync(creatorId, "Liked Public Art", true);
            var privateArtId = await SeedArtAsync(creatorId, "Liked Private Art", false);
            await SeedLikeAsync(ownerId, publicArtId);
            await SeedLikeAsync(ownerId, privateArtId);

            var anonymousClient = _factory.CreateClient();
            var anonymousLiked = await anonymousClient.GetFromJsonAsync<List<ArtDto>>($"/api/v2/accounts/{ownerId}/liked-art");
            Assert.Single(anonymousLiked!);
            Assert.Equal("Liked Public Art", anonymousLiked![0].Title);

            var ownerClient = _factory.CreateClient();
            ownerClient.DefaultRequestHeaders.Add("Cookie", "GoogleOAuth=likes-owner-sub");
            var ownerLiked = await ownerClient.GetFromJsonAsync<List<ArtDto>>($"/api/v2/accounts/{ownerId}/liked-art");
            Assert.Equal(2, ownerLiked!.Count);
        }

        [Fact]
        public async Task ArtUpload_RequiresAuth_AndCreatesArtForAuthenticatedUser()
        {
            var ownerId = await SeedArtistAsync("upload-sub", "UploadUser", "upload@example.com");
            var request = new CreateArtRequest
            {
                Title = "Uploaded",
                IsPublic = true,
                PixelGrid = new MyTestVueApp.Server.Entities.PixelGrid
                {
                    Width = 1,
                    Height = 1,
                    BackgroundColor = "FFFFFF",
                    EncodedGrid = "FFFFFF"
                }
            };

            var anonymousClient = _factory.CreateClient();
            var unauthorized = await anonymousClient.PostAsJsonAsync("/api/v2/art", request);
            Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);

            var ownerClient = _factory.CreateClient();
            ownerClient.DefaultRequestHeaders.Add("Cookie", "GoogleOAuth=upload-sub");
            var createdResponse = await ownerClient.PostAsJsonAsync("/api/v2/art", request);
            createdResponse.EnsureSuccessStatusCode();
            var created = await createdResponse.Content.ReadFromJsonAsync<ArtDto>();

            Assert.NotNull(created);
            Assert.Equal("Uploaded", created!.Title);
            Assert.Contains(ownerId, created.ArtistId);
        }

        private async Task<int> SeedArtistAsync(string subId, string name, string email)
        {
            await using var connection = new NpgsqlConnection(_factory.ConnectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(@"
                INSERT INTO artist (subid, name, email, isadmin, creationdate, privateprofile, notificationsenabled)
                VALUES (@SubId, @Name, @Email, false, now(), false, 63)
                ON CONFLICT (subid) DO UPDATE SET name = excluded.name
                RETURNING id;", connection);
            command.Parameters.AddWithValue("@SubId", subId);
            command.Parameters.AddWithValue("@Name", name);
            command.Parameters.AddWithValue("@Email", email);
            return (int)(await command.ExecuteScalarAsync())!;
        }

        private async Task<int> SeedArtAsync(int artistId, string title, bool isPublic)
        {
            await using var connection = new NpgsqlConnection(_factory.ConnectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(@"
                WITH inserted AS (
                    INSERT INTO art (title, ispublic, creationdate, isgif, gifid, gifframenum, pointid, width, height, encode)
                    VALUES (@Title, @IsPublic, now(), false, 0, 0, 0, 1, 1, 'FFFFFF')
                    RETURNING id
                )
                INSERT INTO contributingartists (artistid, artid)
                SELECT @ArtistId, id FROM inserted
                RETURNING artid;", connection);
            command.Parameters.AddWithValue("@Title", title);
            command.Parameters.AddWithValue("@IsPublic", isPublic);
            command.Parameters.AddWithValue("@ArtistId", artistId);
            return (int)(await command.ExecuteScalarAsync())!;
        }

        private async Task SeedLikeAsync(int artistId, int artId)
        {
            await using var connection = new NpgsqlConnection(_factory.ConnectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(@"
                INSERT INTO likes (artistid, artid)
                VALUES (@ArtistId, @ArtId)
                ON CONFLICT DO NOTHING;", connection);
            command.Parameters.AddWithValue("@ArtistId", artistId);
            command.Parameters.AddWithValue("@ArtId", artId);
            await command.ExecuteNonQueryAsync();
        }
    }
}
