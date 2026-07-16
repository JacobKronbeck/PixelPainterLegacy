const configuredApiBaseUrl = import.meta.env.VITE_API_BASE_URL?.replace(/\/+$/, "") ?? "";

// Production requests intentionally stay on the frontend origin. Vercel proxies
// them to Render so the OAuth session cookie remains first-party in the browser.
const apiBaseUrl = ["localhost", "127.0.0.1"].includes(window.location.hostname)
  ? configuredApiBaseUrl
  : "";

export function apiUrl(path: string): string {
  if (/^https?:\/\//i.test(path)) {
    return path;
  }

  const normalizedPath = path.startsWith("/") ? path : `/${path}`;
  return `${apiBaseUrl}${normalizedPath}`;
}

const retryableStatuses = new Set([500, 502, 503, 504]);

export async function apiFetch(path: string, init: RequestInit = {}): Promise<Response> {
  const method = (init.method ?? "GET").toUpperCase();
  const canRetry = method === "GET" || method === "HEAD";
  const request = () => fetch(apiUrl(path), {
    credentials: "include",
    ...init
  });

  const response = await request();
  if (!canRetry || !retryableStatuses.has(response.status)) {
    return response;
  }

  // Render performs rolling deploys for this monorepo. A read can briefly hit
  // an instance whose database connection is not ready while the next request
  // succeeds. Retry only idempotent reads; never replay a mutation here.
  await new Promise((resolve) => window.setTimeout(resolve, 350));
  return request();
}
