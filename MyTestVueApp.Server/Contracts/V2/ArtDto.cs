namespace MyTestVueApp.Server.Contracts.V2
{
    public record PixelGridDto(int Width, int Height, string BackgroundColor, string EncodedGrid);

    public record TagDto(int Id, string Name, DateTime CreationDate);

    public record ArtDto(
        int Id,
        int[] ArtistId,
        string[] ArtistName,
        string Title,
        bool IsPublic,
        DateTime CreationDate,
        PixelGridDto PixelGrid,
        TagDto[] Tags,
        bool IsGif,
        int GifID,
        int GifFrameNum,
        int GifFps,
        int NumLikes,
        int NumDislikes,
        int NumComments,
        int PointId,
        string PointTitle,
        int ArtspaceId,
        string ArtspaceTitle,
        bool CurrentUserIsOwner);
}
