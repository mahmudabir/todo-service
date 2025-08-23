using FluentValidation;

using Shared.Models.Todos;

namespace Application.Validations.Todos;

public class TodoUpdateValidator : AbstractValidator<TodoUpdateViewModel>
{
    public TodoUpdateValidator()
    {
        RuleFor(x => x).NotNull();
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty();
        RuleFor(x => x.Priority).NotEmpty().GreaterThan(0).LessThanOrEqualTo(5);
    }
}