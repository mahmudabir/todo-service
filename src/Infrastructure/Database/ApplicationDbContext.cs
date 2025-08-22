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
    ILogger<ApplicationDbContext> logger)
    : IdentityDbContext<ApplicationUser, IdentityRole, string>(options),
      IUnitOfWork
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Todo>().ToTable(Tables.Todos);

        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
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