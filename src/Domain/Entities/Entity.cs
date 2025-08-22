namespace Domain.Entities;

public abstract class Entity
{
    public long Id { get; set; }
    public Guid RowId { get; set; } = Guid.CreateVersion7(); // For Audit Logs
}
