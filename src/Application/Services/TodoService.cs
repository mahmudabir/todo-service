using Domain.Abstractions.Services;

using Shared.Models.Results;
using Shared.Models.Todos;

namespace Application.Services;

public class TodoService: ITodoService
{

    public async Task<Result<TodoViewModel>> GetTodoAsync(long id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}