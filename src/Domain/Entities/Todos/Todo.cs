using System.ComponentModel.DataAnnotations.Schema;

using Domain.Entities.Users;

namespace Domain.Entities.Cities;

public class Todo : Entity
{

    [ForeignKey(nameof(User))]
    public string UserId { get; set; }

    public ApplicationUser? User { get; set; }
}