using Shared.Models.Results;
using Shared.Models.Todos;

namespace Domain.Abstractions.Services;

public interface ITodoService
{
    Task<Result<TodoViewModel>> GetTodoAsync(long id, CancellationToken cancellationToken = default);
}