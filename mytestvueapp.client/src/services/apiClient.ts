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

export function apiFetch(path: string, init: RequestInit = {}): Promise<Response> {
  return fetch(apiUrl(path), {
    credentials: "include",
    ...init
  });
}
