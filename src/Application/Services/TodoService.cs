using System.Linq.Expressions;

using AutoMapper;

using Domain.Abstractions.Database.Repositories;
using Domain.Abstractions.Services;
using Domain.Entities.Todos;

using FluentValidation;

using Microsoft.EntityFrameworkCore;

using Shared.Models.Results;
using Shared.Models.Todos;
using Shared.Pagination;

namespace Application.Services;

public class TodoService(ITodoRepository repository, IHttpContextService httpContextService, IMapper mapper)
    : ITodoService
{
    private string? CurrentUserId => httpContextService.GetCurrentUserIdentity();

    public async Task<Result<PagedData<TodoViewModel>>> GetTodosAsync(string? query, Pageable pageable, Sortable sortable, CancellationToken cancellationToken = default)
    {
        Expression<Func<Todo, bool>> predicate = x => (string.IsNullOrEmpty(query)
                                                    || EF.Functions.Like(x.Title, $"%{query}%")
                                                    || (x.Description != null && EF.Functions.Like(x.Description, $"%{query}%")));

        var paged = await repository.GetAllPagedAsync(predicate, pageable, sortable, true, cancellationToken);

        var vm = paged.Content.Select(e => mapper.Map<TodoViewModel>(e)).ToList();
        var resultPaged = new PagedData<TodoViewModel>(paged)
        {
            Content = vm
        };

        return Result<PagedData<TodoViewModel>>.Success()
                                               .WithPayload(resultPaged)
                                               .WithMessageLogic(r => r.Payload!.Content.Any())
                                               .WithSuccessMessage("Todos retrieved")
                                               .WithErrorMessage("No todos found");
    }

    public async Task<Result<TodoViewModel>> GetTodoAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await repository.GetByIdAsync(id, cancellationToken);
        if (entity is null || !IsOwner(entity))
        {
            return Result<TodoViewModel>.Error()
                                        .WithMessage("Todo not found");
        }

        return Result<TodoViewModel>.Success()
                                    .WithPayload(mapper.Map<TodoViewModel>(entity))
                                    .WithMessage("Todo found");
    }

    public async Task<Result<TodoViewModel>> CreateTodoAsync(TodoCreateViewModel model, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(CurrentUserId)) // For Extra Security
        {
            return Result<TodoViewModel>.Error().WithMessage("Unauthorized");
        }

        var entity = mapper.Map<Todo>(model);
        entity.Title = entity.Title.Trim();
        entity.Description = entity.Description?.Trim();
        entity.DueDateUtc = entity.DueDateUtc?.ToUniversalTime();
        entity.UserId = CurrentUserId!;

        await repository.AddAsync(entity, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return Result<TodoViewModel>.Success()
                                    .WithPayload(mapper.Map<TodoViewModel>(entity))
                                    .WithMessage("Todo created");
    }

    public async Task<Result<TodoViewModel>> UpdateTodoAsync(long id, TodoUpdateViewModel model, CancellationToken cancellationToken = default)
    {
        var entity = await repository.GetByIdAsync(id, cancellationToken);
        if (entity is null || !IsOwner(entity)) // For Extra Security
        {
            return Result<TodoViewModel>.Error().WithMessage("Todo not found");
        }

        entity.Title = model.Title.Trim();
        entity.Description = model.Description?.Trim();
        entity.Priority = model.Priority;
        entity.DueDateUtc = model.DueDateUtc?.ToUniversalTime();
        entity.Status = model.Status;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await repository.Update(entity);
        await repository.SaveChangesAsync(cancellationToken);

        return Result<TodoViewModel>.Success()
                                    .WithPayload(mapper.Map<TodoViewModel>(entity))
                                    .WithMessage("Todo updated");
    }

    public async Task<Result<bool>> DeleteTodoAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await repository.GetByIdAsync(id, cancellationToken);
        if (entity is null || !IsOwner(entity)) // For Extra Security
        {
            return Result<bool>.Error().WithMessage("Todo not found");
        }

        await repository.Remove(entity);
        await repository.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success()
                           .WithPayload(true)
                           .WithMessage("Todo deleted");
    }

    private bool IsOwner(Todo entity) => entity.UserId == CurrentUserId;
}