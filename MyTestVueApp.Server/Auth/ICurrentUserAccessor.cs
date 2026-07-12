using MyTestVueApp.Server.Entities;

namespace MyTestVueApp.Server.Auth
{
    public interface ICurrentUserAccessor
    {
        Task<Artist?> GetCurrentUserAsync(HttpContext httpContext);
    }
}
