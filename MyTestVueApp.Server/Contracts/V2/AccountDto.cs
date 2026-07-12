namespace MyTestVueApp.Server.Contracts.V2
{
    public record AccountDto(
        int Id,
        string Name,
        bool IsAdmin,
        bool PrivateProfile,
        DateTime CreationDate,
        string Email,
        int NotificationsEnabled);
}
