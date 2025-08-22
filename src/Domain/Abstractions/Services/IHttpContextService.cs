namespace Domain.Abstractions.Services;

public interface IHttpContextService
{
    public string GetAcceptLanguage();

    public string? GetCurrentUserIdentity();
}