import { apiFetch } from './apiClient';
export default class CommentLikeService {
    public static async insertCommentLike(commentId: number): Promise<boolean> {
        try {
            const response = await apiFetch(`/commentlike/InsertCommentLike?commentId=${commentId}`, {
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
    public static async removeCommentLike(commentId: number): Promise<boolean> {
        try {
            const response = await apiFetch(`/commentlike/RemoveCommentLike?commentId=${commentId}`, {
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
    public static async isCommentLiked(commentId: number): Promise<boolean> {
        try {
            const response = await apiFetch(`/commentlike/IsCommentLiked?commentId=${commentId}`);
            if (!response.ok) {
                throw new Error("Response was false.");
            }
            const isCommentLiked: boolean = (await response.json()) as boolean;
            return isCommentLiked;
        } catch (error) {
            console.error(error);
            return false;
        }
    }
}
