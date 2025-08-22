

using Shared.Models.Results;

namespace Domain.Errors;

public static class FileErrors
{
    public static Error ReadFailed(params string[] messages) => Error.Create("File.ReadFailed", messages);

    public static Error SaveFailed(params string[] messages) => Error.Create("File.SaveFailed", messages);

    public static Error DeleteFailed(params string[] messages) => Error.Create("City.DeleteFailed", messages);
}