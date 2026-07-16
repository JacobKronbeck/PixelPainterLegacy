# Render API Deployment

Pixel Painter Legacy should keep the Vue frontend on Vercel and run the ASP.NET Core API as a separate Render Web Service.

## Host Choice

Use a Render Docker Web Service for the production API.

- Workspace plan: Hobby
- Service compute: Free Web Service
- Runtime: Docker
- Dockerfile path: `MyTestVueApp.Server/Dockerfile`
- Suggested service name: `pixel-painter-legacy-api`
- Start command: leave blank; the Dockerfile entrypoint binds ASP.NET Core to Render's `PORT`

### Prevent frontend-only API restarts

The frontend and API share this repository, but a Vue-only commit must not
restart the Render API. In the Render service dashboard, open **Settings →
Build & Deploy → Build Filters** and add this included path:

```text
MyTestVueApp.Server/**
```

Without this filter, every frontend fix restarts the API. A restart can cause
brief database errors during the rollout and invalidates existing login cookies
until data-protection keys are persisted.

Render is a good fit here because its Hobby workspace includes web services, Docker builds, Git-based deploys, HTTPS, custom domains, environment variables, and WebSockets for SignalR. Keep the API on Free compute first; upgrade only if cold starts, memory, CPU, or bandwidth become a real problem.

## Hobby Scope

Keep the deployed stack inside Render Hobby by:

- Running only one Render service for the ASP.NET API.
- Keeping the Vue frontend on Vercel.
- Keeping Postgres on Supabase instead of adding a Render Postgres database.
- Avoiding background workers, cron jobs, persistent disks, and extra Render services until there is a concrete need.
- Staying within the included Hobby bandwidth/build limits for early testing.

## Required API Environment Variables

Configure these on the Render service:

```text
ApplicationConfiguration__ConnectionString
ApplicationConfiguration__ClientId
ApplicationConfiguration__ClientSecret
ApplicationConfiguration__RedirectUrl
ApplicationConfiguration__PostLoginRedirectUrl
ApplicationConfiguration__OAuthRedirectUrl
ASPNETCORE_ENVIRONMENT
```

Use the Supabase pooled Postgres connection string for `ApplicationConfiguration__ConnectionString`.

Set `ASPNETCORE_ENVIRONMENT=Production`. The API validates its database and OAuth configuration at startup and intentionally refuses to start when required production settings are missing.

Use your frontend origin for `ApplicationConfiguration__RedirectUrl` and
`ApplicationConfiguration__PostLoginRedirectUrl`, including a trailing slash:

```text
https://pixel-painter-legacy.vercel.app/
```

OAuth must return through the Vercel proxy so the session cookie belongs to the
same origin as the frontend. Configure Render with:

```text
ApplicationConfiguration__OAuthRedirectUrl=https://pixel-painter-legacy.vercel.app/api/v2/auth/callback
```

In Google Cloud Console, configure this exact Authorized redirect URI:

```text
https://pixel-painter-legacy.vercel.app/api/v2/auth/callback
```

The API now uses an encrypted and signed ASP.NET Core authentication ticket named `PixelPainterAuth`. The old `GoogleOAuth` cookie is ignored in Production. OAuth requests also require a matching short-lived `PixelPainterOAuthState` cookie, so an authorization callback cannot be used without first starting the login flow.

Render's ephemeral filesystem means authentication tickets can be invalidated by a service restart or deployment because ASP.NET Core data-protection keys are regenerated. This logs users out but does not expose their sessions. Persist data-protection keys before adding multiple API instances or requiring sessions to survive deployments.

## Vercel Follow-Up

Keep production API calls on the Vercel origin. Do not set `VITE_API_BASE_URL`
for the production deployment; `vercel.json` proxies API and SignalR requests
to Render while preserving a first-party browser session.

## Release Gate

Before promoting a commit:

1. Run the Supabase migrations and verify the `artist` uniqueness indexes.
1. Confirm the Google OAuth client has the exact Vercel callback above.
1. Confirm all required Render environment variables are present.
1. Wait for the GitHub `Verify Web Application` workflow to pass the frontend build, API build, integration tests, and Docker image build.
1. Test Google login through the Vercel URL, then test account lookup, art creation, comments, reactions, tags, maps, and logout.
1. Keep the prior Render deploy and Vercel deployment available for rollback.
