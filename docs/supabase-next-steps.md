# Supabase Next Steps

This repo is moving from local SQL Server toward Supabase Postgres.

## Already Prepared

- `supabase/migrations/20260707190000_initial_schema.sql` defines the first Postgres schema.
- `MyTestVueApp.Server` now references `Npgsql` instead of `Microsoft.Data.SqlClient`.
- `MyTestVueApp.Server/appsettings.Supabase.example.json` shows the Supabase connection-string shape for the ASP.NET API.
- `MyTestVueApp.Server/Database/PostgresDataAccess.cs` provides a small Postgres helper for shared query, scalar, execute, and transaction flows.
- `TagService` and the notification mark/update methods now use the shared Postgres helper instead of opening raw connections directly.

## Supabase Dashboard

The Vercel integration currently syncs Supabase variables to the Vercel project, but this app is a Vite app. If you expose public variables to the browser, set the public prefix to:

```text
VITE_
```

Do not expose the database password or service-role key to the Vite client.

In Supabase, go to **Project Settings -> Integrations -> Vercel Integration -> Manage project connection** and change **Customize public environment variable prefix** from `NEXT_PUBLIC_` to `VITE_`, then save. After saving, trigger a fresh Vercel deployment so the synced variable names update.

Browser-safe names should look like:

```text
VITE_SUPABASE_URL
VITE_SUPABASE_ANON_KEY
```

Server-only secrets should stay unprefixed and should only be configured on the ASP.NET API host:

```text
ApplicationConfiguration__ConnectionString
```

## Important Deployment Note

Vercel is currently building only the static Vue frontend from `mytestvueapp.client/dist`. The ASP.NET API still needs a host that can run .NET. The best low-cost candidates for this app are Azure App Service, Render, Fly.io, or Railway.

The frontend uses relative API paths like `/artaccess/GetAllArt`, so production needs one of these:

- Host the ASP.NET API at the same origin as the frontend.
- Add Vercel rewrites from API paths to the deployed ASP.NET API URL.
- Refactor the frontend to call a `VITE_API_BASE_URL`.

## Remaining Migration Work

- Convert SQL Server-specific query syntax in services:
  - `[Name]` and `[Message]`
  - `[PixelPainter].[dbo].Table`
  - `SELECT TOP (1)`
  - `SCOPE_IDENTITY()` and `@@IDENTITY`
  - `OUTPUT INSERTED.Id`
  - `ISNULL(...)`
  - boolean comparisons such as `isPublic = 1`
- Cast Postgres count results where code expects `int`, for example `count(*)::int`.
- Convert map geometry calls from `Shape.STAsText()` to `ST_AsText(shape)`.
- Continue moving service clusters from raw `SqlConnection` / `SqlCommand` usage to `IPostgresDataAccess`.
- Decide whether to import the large SQL Server seed data or start Supabase with a clean database plus the required `Public Grid` artist.
- Polish Swagger/OpenAPI for the ASP.NET API surface once the service migration stabilizes.
