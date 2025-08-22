using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Shared.Models.Results;
using Shared.Models.Todos;
using Shared.Pagination;

namespace WebApi.Controllers.Todos;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TodosController(IServiceProvider services) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string query,
                                         [FromQuery] Pageable pageable,
                                         [FromQuery] Sortable sortable,
                                         CancellationToken ct = default)
    {
        return Ok();
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<Result<TodoViewModel>>> GetById(long id,
                                                                   CancellationToken ct = default)
    {
        return Ok();
    }

    [HttpPost]
    public async Task<ActionResult<Result<TodoViewModel>>> Post([FromForm] TodoViewModel payload,
                                                                IFormFile image,
                                                                CancellationToken cancellationToken)
    {
        return Ok();
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<TodoViewModel>> Put([FromRoute] long id,
                                                       [FromBody] TodoViewModel payload,
                                                       IFormFile image,
                                                       CancellationToken cancellationToken)
    {
        return Ok();
    }

    [HttpDelete("{id:long}")]
    public async Task<ActionResult<bool>> Delete([FromRoute] long id,
                                                 CancellationToken cancellationToken)
    {
        return Ok();
    }
}