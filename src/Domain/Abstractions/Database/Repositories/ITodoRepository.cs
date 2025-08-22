using Domain.Entities.Todos;

namespace Domain.Abstractions.Database.Repositories;

public interface ITodoRepository : IRepository<Todo, long>
{

}