using System.ComponentModel.DataAnnotations;

using Shared.Enums;

namespace Shared.Models.Todos;

public class TodoUpdateViewModel
{
    public long Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; set; }

    public TodoStatus Status { get; set; } = TodoStatus.Pending;

    public DateTime? DueDateUtc { get; set; }

    [Range(1, 5)]
    public int Priority { get; set; } = 3;

    public bool IsCompleted { get; set; }
}