using MyTestVueApp.Server.Entities;

namespace MyTestVueApp.Server.Contracts.V2
{
    public class CreateArtRequest
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsPublic { get; set; }
        public PixelGrid PixelGrid { get; set; } = new();
        public List<Tag> Tags { get; set; } = new();
    }
}
