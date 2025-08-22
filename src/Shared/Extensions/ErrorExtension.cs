using Shared.Models.Results;

namespace Shared.Extensions;

public static class ErrorExtension
{
    public static IDictionary<string, string[]> AddError(this IDictionary<string, string[]> errors, string key, params string[] messages)
    {
        if (errors.TryGetValue(key, out string[]? currentMessages))
        {
            errors[key] = [..currentMessages, ..messages];
            return errors;
        }
        errors.Add(key, messages);
        return errors;
    }

    public static IDictionary<string, string[]> AddErrors(this IDictionary<string, string[]> errors, IDictionary<string, string[]> newErrors)
    {

        foreach (var keyValuePair in newErrors)
        {
            if (errors.TryGetValue(keyValuePair.Key, out string[]? currentMessages))
            {
                errors[keyValuePair.Key] = [..currentMessages, ..keyValuePair.Value];
                return errors;
            }
            errors.Add(keyValuePair.Key, keyValuePair.Value);
        }
        return errors;
    }

    public static IDictionary<string, string[]> AddError(this IDictionary<string, string[]> errors, Error error)
    {
        if (errors.TryGetValue(error.Key, out string[]? currentMessages))
        {
            errors[error.Key] = [..currentMessages, ..error.Errors[error.Key]];
            return errors;
        }
        errors.Add(error.Key, error.Errors[error.Key]);
        return errors;
    }
}