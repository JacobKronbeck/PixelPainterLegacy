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
```

Use the Supabase pooled Postgres connection string for `ApplicationConfiguration__ConnectionString`.

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

## Vercel Follow-Up

Keep production API calls on the Vercel origin. Do not set `VITE_API_BASE_URL`
for the production deployment; `vercel.json` proxies API and SignalR requests
to Render while preserving a first-party browser session.
