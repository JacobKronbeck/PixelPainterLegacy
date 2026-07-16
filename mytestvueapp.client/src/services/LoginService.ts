import { apiFetch } from './apiClient';
import type Artist from "@/entities/Artist";

export interface AuthSession {
  isAuthenticated: boolean;
  user: Artist;
}

export default class LoginService {
  private static async fetchWithRetry(path: string, init?: RequestInit): Promise<Response> {
    const response = await apiFetch(path, init);
    if (response.status < 500) {
      return response;
    }

    await new Promise((resolve) => window.setTimeout(resolve, 500));
    return apiFetch(path, init);
  }

  public static async isLoggedIn(): Promise<boolean> {
    try {
      const response = await LoginService.fetchWithRetry("/api/v2/auth/me");
      if (response.status === 401) {
        return false;
      }
      if (!response.ok) {
        throw new Error(`Error checking login status (${response.status})`);
      }

      const session = (await response.json()) as AuthSession;
      return session.isAuthenticated;
    } catch (error) {
      console.error("Error checking login status:", error);
      return false;
    }
  }

  public static async logout(): Promise<void> {
    try {
      const response = await apiFetch("/api/v2/auth/logout", { method: "POST" });
      if (!response.ok) {
        throw new Error("Network response was not ok");
      }
    } catch (error) {
      console.error("Error logging out:", error);
    }
  }

  public static async GetArtistByName(name: string): Promise<Artist> {
    try {
      const response = await apiFetch(`/api/v2/accounts/by-name/${encodeURIComponent(name)}`);
      if (!response.ok) {
        throw new Error(`Error retrieving artist (${response.status})`);
      }
      const json = await response.json();

      return json as Artist;
    } catch (error) {
      console.error;
      throw error;
    }
  }

  public static async GetAllArtists(): Promise<Artist[]> {
    try {
      const response = await apiFetch(`/login/GetAllArtists`);
      const json = await response.json();

      const allArtists: Artist[] = [];

      for (const jsonArtist of json) {
        allArtists.push(jsonArtist as Artist);
      }

      return allArtists;
    } catch (error) {
      console.error;
      throw error;
    }
  }

  public static async getCurrentUser(): Promise<Artist> {
    const session = await LoginService.getCurrentSession();
    return session.user;
  }

  public static async getCurrentSession(): Promise<AuthSession> {
    try {
      const response = await LoginService.fetchWithRetry("/api/v2/auth/me");

      if (!response.ok) {
        throw new Error("Error retrieving user");
      }

      return (await response.json()) as AuthSession;
    } catch (error) {
      console.error(error);
      throw error;
    }
  }

  public static async getIsAdmin(): Promise<boolean> {
    try {
      const response = await LoginService.fetchWithRetry("/api/v2/auth/me");
      if (response.status === 401) {
        return false;
      }
      if (!response.ok) {
        throw new Error(`Error checking admin status (${response.status})`);
      }

      const session = (await response.json()) as AuthSession;
      return !!session.user?.isAdmin;
    } catch (error) {
      console.error(error);
      return false;
    }
  }

  public static async setAdmin(artistId: number, isAdmin: boolean): Promise<boolean> {
    try {
      const response = await apiFetch(`/login/SetAdmin?artistId=${artistId}&isAdmin=${isAdmin}`, {
        method: "POST",
        headers: { "Content-Type": "application/json" }
      });
      if (!response.ok) {
        let t = ""; try { t = await response.text(); } catch {}
        throw new Error(`Error setting admin: ${response.status} ${t}`);
      }
      // server returns Ok(); assume true on 200
      return true;
    } catch (error) {
      console.error(error);
      return false;
    }
  }

  public static async updateUsername(newUsername: any): Promise<boolean> {
    try {
      const normalizedUsername = String(newUsername ?? "").trim();
      const response = await apiFetch(
        `/login/UpdateUsername?newUsername=${encodeURIComponent(normalizedUsername)}`,
        {
          method: "PUT",
          headers: { "Content-Type": "application/json" }
        }
      );

      if (!response.ok) {
        throw new Error("Error: Bad response");
      }

      const data = await response.json();
      const success: boolean = data;

      return success;
    } catch (error) {
      console.error;
      return false;
    }
  }
  public static async privateSwitchChange(artistId: Number): Promise<void> {
    try {
      const response = await apiFetch(`/login/privateSwitchChange`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(artistId)
      });

      if (!response.ok) {
        throw new Error(`Error: ${response.status} - ${response.statusText}`);
      }
    } catch (error) {
      console.error;
      throw error;
    }
  }
  public static async deleteArtist(id: number): Promise<void> {
    try {
      const response = await apiFetch(`/login/DeleteArtist?id=${id}`, {
        method: "DELETE",
        headers: { "Content-Type": "application/json" }
      });

      if (!response.ok) {
        throw new Error("Error: Bad response");
      }
    } catch (error) {
      console.error;
      throw error;
    }
  }
  public static async updateNotificationsEnabled(artistId: number, notificationsEnabled: number): Promise<boolean> {
    try {
      const response = await apiFetch("/artist/UpdateNotifications", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ artistId, notificationsEnabled })
      });
      return response.ok;
    } catch {
      return false;
    }
  }
}
