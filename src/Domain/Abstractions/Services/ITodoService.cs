using Shared.Models.Results;
using Shared.Models.Todos;
using Shared.Pagination;

namespace Domain.Abstractions.Services;

public interface ITodoService
{
    Task<Result<TodoViewModel>> GetTodoAsync(long id, CancellationToken cancellationToken = default);
    Task<Result<PagedData<TodoViewModel>>> GetTodosAsync(string? query, Pageable pageable, Sortable sortable, CancellationToken cancellationToken = default);

    Task<Result<TodoViewModel>> CreateTodoAsync(TodoCreateViewModel model, CancellationToken cancellationToken = default);
    Task<Result<TodoViewModel>> UpdateTodoAsync(long id, TodoUpdateViewModel model, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteTodoAsync(long id, CancellationToken cancellationToken = default);
}