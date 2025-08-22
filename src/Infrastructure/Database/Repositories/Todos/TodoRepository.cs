using Domain.Abstractions.Database.Repositories;
using Domain.Entities.Cities;

using Microsoft.Extensions.Logging;

namespace Infrastructure.Database.Repositories.Todos;

public class TodoRepository(ApplicationDbContext dbContext, ILogger<TodoRepository> logger)
    : GenericRepository<Todo, long>(dbContext, logger),
      ITodoRepository;