using Domain.Abstractions.Database.Repositories;
using Domain.Entities.Todos;

using Microsoft.Extensions.Logging;

namespace Infrastructure.Database.Repositories.Todos;

public class TodoRepository(ApplicationDbContext dbContext, ILogger<TodoRepository> logger)
    : GenericRepository<Todo, long>(dbContext, logger),
      ITodoRepository;