using Domain.Entities.Todos;

using Infrastructure.Database;

using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplyUserGlobalQueryFilter(this ModelBuilder modelBuilder, ApplicationDbContext dbContext)
    {
        modelBuilder.Entity<Todo>().HasQueryFilter(t => t.UserId == dbContext.CurrentUserId);
    }
}