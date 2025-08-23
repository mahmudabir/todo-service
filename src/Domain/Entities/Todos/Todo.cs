using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Domain.Entities.Users;
using Shared.Enums;

namespace Domain.Entities.Todos;

public class Todo : Entity
{
    /// <summary>
    /// Title of the todo task.
    /// </summary>
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description or notes.
    /// </summary>
    [MaxLength(4000)]
    public string? Description { get; set; }

    /// <summary>
    /// Current status of the task.
    /// </summary>
    public TodoStatus Status { get; set; } = TodoStatus.Pending;

    /// <summary>
    /// Optional UTC due date.
    /// </summary>
    public DateTime? DueDateUtc { get; set; }

    /// <summary>
    /// Indicates priority (1=Highest, larger number = lower priority).
    /// </summary>
    public int Priority { get; set; } = 3;

    /// <summary>
    /// Soft completion flag (derived convenience of Status == Completed but stored for fast queries).
    /// </summary>
    [NotMapped]
    public bool IsCompleted => Status == TodoStatus.Completed;

    /// <summary>
    /// UTC timestamps for auditing.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(User))]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }
}