# Production Rollout Checklist

## Before Deploying

- [ ] Back up the Supabase database or confirm point-in-time recovery is available.
- [ ] Apply `supabase/migrations` in filename order to the production database.
- [ ] Verify `ux_artist_subid` and `ux_artist_name_lower` exist and are unique.
- [ ] Confirm the Render service uses `ASPNETCORE_ENVIRONMENT=Production`.
- [ ] Confirm the Render connection string uses SSL and the Supabase pooled endpoint.
- [ ] Confirm the Google client ID and secret are configured only on Render.
- [ ] Confirm Google authorizes exactly `https://pixel-painter-legacy.vercel.app/api/v2/auth/callback`.
- [ ] Confirm both frontend redirect settings use `https://pixel-painter-legacy.vercel.app/`.
- [ ] Confirm the GitHub `Verify Web Application` workflow succeeds.

## Preview Smoke Test

- [ ] `/healthz` returns `200` through both Render and the Vercel proxy.
- [ ] Anonymous `/api/v2/auth/me` returns `401`.
- [ ] Google login returns through Vercel and `/api/v2/auth/me` returns the signed-in account.
- [ ] Logout clears the session and `/api/v2/auth/me` returns `401` again.
- [ ] A forged legacy `GoogleOAuth` cookie is rejected.
- [ ] Account lookup and username update work.
- [ ] Public/private art visibility and art creation work.
- [ ] Comments, likes, dislikes, and tags work.
- [ ] Map points and SignalR connections work.

## Promotion and Rollback

- [ ] Record the Git commit, Render deployment, Vercel deployment, and migration version.
- [ ] Promote the tested commit only after the preview smoke test passes.
- [ ] Watch Render errors, Vercel function/proxy errors, and database connections after release.
- [ ] Roll back Render and Vercel to the recorded prior deployments if authentication or data writes fail.
- [ ] Do not roll back a database migration until its down-migration/data impact has been reviewed.
