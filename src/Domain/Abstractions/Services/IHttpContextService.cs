namespace Domain.Abstractions.Services;

public interface IHttpContextService
{
    public string? GetCurrentUserIdentity();
}