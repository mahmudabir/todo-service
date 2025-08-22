using Infrastructure.Database;

using Microsoft.EntityFrameworkCore;

namespace WebApi.Infrastructure.Extensions;

public static class MigrationExtensions
{
    public static void ApplyMigrations(this IApplicationBuilder app)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();
        using ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.EnsureCreated();
        if (dbContext.Database.CanConnect())
        {
            var pendingMigrations = dbContext.Database.GetPendingMigrations();
            if (pendingMigrations.Any())
            {
                //Migrate Database as the database is already there
                dbContext.Database.Migrate();
            }
        }
        else
        {
            var pendingMigrations = dbContext.Database.GetPendingMigrations();
            if (pendingMigrations.Any())
            {
                //First Migrate then ensure Created to avoid database errors
                dbContext.Database.Migrate();

                //Ensures that Database is created
                dbContext.Database.EnsureCreated();
            }
        }
    }
}