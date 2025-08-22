

using Shared.Models.Results;

namespace Domain.Entities.Users;

public static class UserErrors
{
    public static Error NotFound(Guid userId) => Error.Create(
                                                                        "Users.NotFound",
                                                                        $"The user with the Id = '{userId}' was not found");

    public static Error Unauthorized() => Error.Create(
                                                                  "Users.Unauthorized",
                                                                  "You are not authorized to perform this action.");

    public static readonly Error NotFoundByEmail = Error.Create(
                                                                           "Users.NotFoundByEmail",
                                                                           "The user with the specified email was not found");

    public static readonly Error EmailNotUnique = Error.Create(
                                                                          "Users.EmailNotUnique",
                                                                          "The provided email is not unique");

    public static readonly Error AlreadyExists = Error.Create(
                                                                       "Users.AlreadyExists",
                                                                       "User already exists");
}
