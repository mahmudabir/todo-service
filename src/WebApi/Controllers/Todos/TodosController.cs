using Domain.Abstractions.Services;

using FluentValidation;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Shared.Models.Results;
using Shared.Models.Todos;
using Shared.Pagination;

namespace WebApi.Controllers.Todos;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TodosController(ITodoService todoService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Result<PagedData<TodoViewModel>>>> Get([FromQuery] string query,
                                                                          [FromQuery] Pageable pageable,
                                                                          [FromQuery] Sortable sortable,
                                                                          CancellationToken ct = default)
    {
        var result = await todoService.GetTodosAsync(query, pageable, sortable, ct);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<Result<TodoViewModel>>> GetById(long id,
                                                                   CancellationToken ct = default)
    {
        var result = await todoService.GetTodoAsync(id, ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<Result<TodoViewModel>>> Post([FromBody] TodoCreateViewModel payload,
                                                                [FromServices] IValidator<TodoCreateViewModel> validator,
                                                                CancellationToken cancellationToken)
    {
        var validationResult = validator.Validate(payload);
        if (!validationResult.IsValid)
        {
            return Result<TodoViewModel>.Error()
                                        .WithMessage("Validation failure")
                                        .WithErrors(validationResult.ToDictionary());
        }
        var result = await todoService.CreateTodoAsync(payload, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<Result<TodoViewModel>>> Put([FromRoute] long id,
                                                               [FromBody] TodoUpdateViewModel payload,
                                                               [FromServices] IValidator<TodoUpdateViewModel> validator,
                                                               CancellationToken cancellationToken)
    {
        var validationResult = validator.Validate(payload);
        if (!validationResult.IsValid)
        {
            return Result<TodoViewModel>.Error()
                                        .WithMessage("Validation failure")
                                        .WithErrors(validationResult.ToDictionary());
        }

        var result = await todoService.UpdateTodoAsync(id, payload, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:long}")]
    public async Task<ActionResult<Result<bool>>> Delete([FromRoute] long id,
                                                         CancellationToken cancellationToken)
    {
        var result = await todoService.DeleteTodoAsync(id, cancellationToken);
        return Ok(result);
    }
}