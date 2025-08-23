using System.Data;

using Domain.Abstractions.Database;
using Domain.Abstractions.Services;
using Domain.Entities.Todos;
using Domain.Entities.Users;

using Infrastructure.Extensions;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Database;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    IHttpContextService httpContextService,
    ILogger<ApplicationDbContext> logger)
    : IdentityDbContext<ApplicationUser, IdentityRole, string>(options),
      IUnitOfWork
{
    public string? CurrentUserId = httpContextService.GetCurrentUserIdentity();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyUserGlobalQueryFilter(this);

        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        var users = new List<ApplicationUser>
        {
            new ApplicationUser
            {
                Id = "20c5308c-f997-4747-b90c-db3a0e830f81",
                UserName = "user1",
                NormalizedUserName = "USER1",
                Email = "user1@example.com",
                NormalizedEmail = "USER1@EXAMPLE.COM",
                SecurityStamp = "e2986ff7-3cb6-4fd9-ad4e-7665c01db1dd",
                ConcurrencyStamp = "acc11141-6c70-4ec5-a33c-f9df6fadd952",
                PasswordHash = "AQAAAAIAAYagAAAAEE5HvjKPN+CAsg8Wr0rqrgbIMw7AjxT9qhFMLS+ZtCeF1Y3nkOAs00jD0MHtRbYpyQ==", // user1@1A
            },
            new ApplicationUser
            {
                Id = "a1fee97a-5b19-46d4-ab61-29608d0e793a",
                UserName = "user2",
                NormalizedUserName = "USER2",
                Email = "user2@example.com",
                NormalizedEmail = "USER2@EXAMPLE.COM",
                SecurityStamp = "64897df1-a13e-40fd-a09b-008cceb3f934",
                ConcurrencyStamp = "6d9d8935-0ab4-4b5e-9acf-134788e7989e",
                PasswordHash = "AQAAAAIAAYagAAAAEMgmC5odpySCRAN03w3ynkzPsOcrg2Y/9Nxu2LKlroYlDlsJxj6ZugR5VcOtEguo6w==", // user2@1A
            }
        };

        // Seed initial Data
        modelBuilder.Entity<ApplicationUser>().HasData(users);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Configure all enum properties to be stored as strings
        configurationBuilder
            .Properties<Enum>()
            .HaveConversion<string>();

        base.ConfigureConventions(configurationBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // When should you publish domain events?
        //
        // 1. BEFORE calling SaveChangesAsync
        //     - domain events are part of the same transaction
        //     - immediate consistency
        // 2. AFTER calling SaveChangesAsync
        //     - domain events are a separate transaction
        //     - eventual consistency
        //     - handlers can fail

        int result;
        try
        {
            result = await base.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "An error occurred during the transaction.");
            throw;
        }

        return result;
    }

    private IDbContextTransaction? _currentTransaction;

    #region SqlServer

    // For Sql Server
    public async Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        await BeginTransactionAsync(cancellationToken);
        try
        {
            await operation();
            await CommitTransactionAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "An error occurred during the transaction.");
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    // For Sql Server
    public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation, CancellationToken cancellationToken = default)
    {
        await BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await operation();
            await CommitTransactionAsync(cancellationToken);
            return result;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "An error occurred during the transaction.");
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    private async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
            throw new InvalidOperationException("A transaction is already in progress.");

        _currentTransaction = await Database.BeginTransactionAsync(cancellationToken);
    }

    private async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
            throw new InvalidOperationException("No transaction in progress.");

        await _currentTransaction.CommitAsync(cancellationToken);
        await _currentTransaction.DisposeAsync();
        _currentTransaction = null;
    }

    private async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
            throw new InvalidOperationException("No transaction in progress.");

        await _currentTransaction.RollbackAsync(cancellationToken);
        await _currentTransaction.DisposeAsync();
        _currentTransaction = null;
    }

    #endregion SqlServer
}