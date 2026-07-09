using MyTestVueApp.Server.Auth;
using MyTestVueApp.Server.Contracts.V2;
using MyTestVueApp.Server.Entities;

namespace MyTestVueApp.Server.Interfaces
{
    public interface IV2AccountService
    {
        Task<AccountDto> GetOrCreateFromGoogleAsync(GoogleUserInfo googleUser);
        Task<AccountDto?> GetBySubIdAsync(string subId);
        Task<AccountDto?> GetByNameAsync(string name);
        AccountDto ToDto(Artist artist);
    }
}
