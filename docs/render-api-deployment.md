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
```

Use the Supabase pooled Postgres connection string for `ApplicationConfiguration__ConnectionString`.

Use your frontend origin for `ApplicationConfiguration__RedirectUrl` and
`ApplicationConfiguration__PostLoginRedirectUrl`, including a trailing slash:

```text
https://your-vercel-frontend-host/
```

The API derives Google's OAuth callback URL from the public API request host by default. In Google Cloud Console, the Authorized redirect URI must be:

```text
https://your-render-api-host/login/LoginRedirect
```

If you ever need to force that callback instead of deriving it, set:

```text
ApplicationConfiguration__OAuthRedirectUrl=https://your-render-api-host/login/LoginRedirect
```

## Vercel Follow-Up

After Render provisions the API, copy the Render service URL and set this on the Vercel frontend project:

```text
VITE_API_BASE_URL=https://your-render-api-host
```

Redeploy the Vercel project after saving the variable.
