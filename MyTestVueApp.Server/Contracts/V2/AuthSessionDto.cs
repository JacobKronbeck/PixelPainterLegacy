namespace MyTestVueApp.Server.Contracts.V2
{
    public record AuthSessionDto(bool IsAuthenticated, AccountDto User);
}
