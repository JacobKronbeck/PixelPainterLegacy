import { apiFetch } from './apiClient';
export default class DislikeService {
    public static async insertDislike(artId: number): Promise<boolean> {
        try {
            const response = await apiFetch(`/dislike/InsertDislike?artId=${artId}`, {
                method: "POST",
                headers: { "Content-Type": "application/json" }
            });
            if (!response.ok) {
                throw new Error("Response was false.");
            }
            return true;
        } catch (error) {
            console.error(error);
            return false;
        }
    }
    public static async removeDislike(artId: number): Promise<boolean> {
        try {
            const response = await apiFetch(`/dislike/RemoveDislike?artId=${artId}`, {
                method: "DELETE",
                headers: { "Content-Type": "application/json" }
            });
            if (!response.ok) {
                throw new Error("Response was false.");
            }
            return true;
        } catch (error) {
            console.error(error);
            return false;
        }
    }
    public static async isDisliked(artId: number): Promise<boolean> {
        try {
            const response = await apiFetch(`/dislike/IsDisliked?artId=${artId}`);
            if (!response.ok) {
                throw new Error("Response was false.");
            }
            const isDisliked: boolean = (await response.json()) as boolean;
            return isDisliked;
        } catch (error) {
            console.error(error);
            return false;
        }
    }
}
