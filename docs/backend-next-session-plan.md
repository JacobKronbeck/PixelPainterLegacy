# Backend Next Session Plan

## Current State

- Branch: `Backend-Restructure`
- Recent commits:
  - `ca92df4 Fix Postgres service queries`
  - `afcb416 Add Postgres data access helper`
  - `d63aa16 Finish Postgres backend cleanup`
  - `7cd84c3 Add Supabase project scaffold`
- Working tree was clean after the last commit.
- `dotnet` was not available on PATH in the Codex shell, so no build verification has been run yet.

## What Is Done

- Added initial Supabase local config and migration:
  - `supabase/config.toml`
  - `supabase/migrations/20260707190000_initial_schema.sql`
- Added Supabase connection-string example:
  - `MyTestVueApp.Server/appsettings.Supabase.example.json`
- Swapped backend package reference from `Microsoft.Data.SqlClient` to `Npgsql`.
- Added SQL alias bridge:
  - `MyTestVueApp.Server/Database/SqlClientAliases.cs`
- Added shared Postgres data-access helper:
  - `MyTestVueApp.Server/Database/IPostgresDataAccess.cs`
  - `MyTestVueApp.Server/Database/PostgresDataAccess.cs`
- Registered helper in `Program.cs`.
- Migrated `TagService` to `IPostgresDataAccess`.
- Migrated notification mark/update methods to `IPostgresDataAccess`.
- Cleaned many SQL Server-specific queries for Postgres syntax.
- Added Vite env typings for:
  - `VITE_SUPABASE_URL`
  - `VITE_SUPABASE_ANON_KEY`
  - `VITE_API_BASE_URL`

## Immediate Next Steps

1. Restore build verification.
   - Make `dotnet` available on PATH.
   - Run:
     ```powershell
     dotnet restore
     dotnet build MyTestVueApp.Server/MyTestVueApp.Server.csproj
     ```
   - Fix compile errors before deeper refactors.

2. Migrate the small reaction service cluster to `IPostgresDataAccess`.
   - `LikeService`
   - `DislikeService`
   - `CommentLikeService`
   - `CommentDislikeService`
   - Commit message suggestion: `Migrate reaction services to data helper`

3. Polish Swagger/OpenAPI.
   - Swagger is already wired in `Program.cs`.
   - Add API metadata such as title, version, and description.
   - Add useful response annotations on controllers where obvious.
   - Decide whether Swagger UI should remain development-only or be enabled on the deployed API host.
   - Commit message suggestion: `Polish Swagger API metadata`

4. Decide production API hosting.
   - Vercel can host the Vue frontend, not the ASP.NET API.
   - Choose a .NET host such as Azure App Service, Render, Fly.io, or Railway.
   - Then either:
     - configure Vercel rewrites to the API host, or
     - refactor client services to use `VITE_API_BASE_URL`.

5. Run the Supabase migration against a real project.
   - Confirm PostGIS extension support.
   - Confirm the `Public Grid` seed artist exists.
   - Smoke-test login, art save/load, comments, likes, tags, and map points.

## Later Backend Migration Order

1. Reaction services:
   - `LikeService`
   - `DislikeService`
   - `CommentLikeService`
   - `CommentDislikeService`

2. Identity/profile services:
   - `LoginService`
   - `ArtistService`
   - `FriendsService`

3. Map services:
   - `MapAccessService`

4. Large art workflow:
   - `ArtAccessService`
   - GIF save/update paths
   - contributing artists
   - tags integration

## Commit Guidance

- Keep commits frequent and scoped.
- Prefer messages like:
  - `Migrate reaction services to data helper`
  - `Fix backend build after Npgsql migration`
  - `Polish Swagger API metadata`
  - `Add Vercel API base URL support`
- Avoid mixing service refactors, deployment config, and frontend changes in the same commit unless they are inseparable.

