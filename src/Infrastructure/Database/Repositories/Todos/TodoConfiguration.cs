using Domain.Entities.Todos;

using Shared.Enums;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Database.Repositories.Todos;

public class TodoConfiguration : IEntityTypeConfiguration<Todo>
{
    public void Configure(EntityTypeBuilder<Todo> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Title).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Description).HasMaxLength(4000);
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(t => t.Priority).HasDefaultValue(3);
        builder.Property(t => t.IsCompleted).HasDefaultValue(false);
        builder.Property(t => t.CreatedAtUtc);
        builder.Property(t => t.UpdatedAtUtc);
    }
}