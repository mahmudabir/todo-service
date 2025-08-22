using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace WebApi.Infrastructure.Extensions;

public static class HelperExtensions
{
    public static IDictionary<string, string[]?> ToFluentErrors(this ModelStateDictionary modelState)
    {
        return modelState.ToDictionary(ms => ms.Key, ms => ms.Value?.Errors.Select(e => e.ErrorMessage).ToArray());
    }
}