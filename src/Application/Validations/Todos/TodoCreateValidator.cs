using FluentValidation;

using Shared.Models.Todos;

namespace Application.Validations.Todos;

public class TodoCreateValidator : AbstractValidator<TodoCreateViewModel>
{
    public TodoCreateValidator()
    {
        RuleFor(x => x).NotNull();
        RuleFor(x => x.DueDateUtc).GreaterThanOrEqualTo(DateTime.UtcNow);
        RuleFor(x => x.Title).NotEmpty();
        RuleFor(x => x.Priority).NotEmpty().GreaterThan(0).LessThanOrEqualTo(5);
    }
}