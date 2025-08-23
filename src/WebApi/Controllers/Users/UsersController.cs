using System.Linq.Expressions;

using Domain.Entities.Users;

using Infrastructure.Database;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Shared.Constants;
using Shared.Extensions;
using Shared.Models.Results;
using Shared.Models.Users;

using WebApi.Infrastructure.Extensions;

namespace WebApi.Controllers.Users;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Result<List<User>>>> Get([FromQuery] string q = null, CancellationToken ct = default)
    {
        Expression<Func<ApplicationUser, bool>> predicate = (x) => string.IsNullOrEmpty(q) || (EF.Functions.Like(x.UserName, $"%{q}%") ||
                                                                                               EF.Functions.Like(x.Email, $"%{q}%") ||
                                                                                               EF.Functions.Like(x.PhoneNumber, $"%{q}%"));

        var queryable = context.Users
                               .Where(predicate);

        queryable = queryable.OrderBy(x => x.UserName);

        var users = await queryable.Select(_userSelector)
                                   .ToListAsync(ct);

        var result = Result<List<User>>.Create(users.Count > 0)
                                       .WithPayload(users)
                                       .WithMessageLogic(x => x.Payload?.Count > 0)
                                       .WithSuccessMessage("Data found")
                                       .WithErrorMessage("Data not found");

        return Ok(result);
    }

    [HttpGet("{username}")]
    public async Task<ActionResult<Result<User>>> GetByUsername(string username, CancellationToken ct = default)
    {
        User? user = await context.Users
                                  .Where(x => x.UserName == username)
                                  .Select(_userSelector)
                                  .FirstOrDefaultAsync(ct);

        var result = Result<User>.Create(user != null)
                                 .WithPayload(user)
                                 .WithMessageLogic(x => x.Payload != null)
                                 .WithSuccessMessage("User found")
                                 .WithErrorMessage("User not found");

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<Result<User>>> CreateUser(User userRegistration, CancellationToken ct = default)
    {
        if (!TryValidateModel(userRegistration))
        {
            return Ok(Result<User>.Error()
                                  .WithMessage("Validation failure")
                                  .WithErrors(ModelState.ToFluentErrors())
                     );
        }

        if (string.IsNullOrEmpty(userRegistration.Password))
        {
            return Ok(Result<User>.Error()
                                  .WithMessage("Validation failure")
                                  .WithError(new KeyValuePair<string, string[]>("password", ["Password field is required"]))
                     );
        }

        ApplicationUser identityUser = new ApplicationUser
        {
            UserName = userRegistration.Username,
            Email = userRegistration.Email,
            PhoneNumber = userRegistration.PhoneNumber,
        };

        var userResult = await userManager.CreateAsync(identityUser, userRegistration.Password);

        var result = Result<User>.Create(userResult.Succeeded)
                                 .WithPayload(new User
                                 {
                                     Username = userRegistration.Username,
                                     Email = userRegistration.Email
                                 })
                                 .WithMessageLogic(_ => userResult.Succeeded)
                                 .WithErrorMessage("User creation failed")
                                 .WithSuccessMessage("User created");
        return Ok(result);
    }

    [HttpPut("{username}")]
    public async Task<ActionResult<Result<User>>> UpdateUser(string username, User userRegistration, CancellationToken ct = default)
    {
        if (!TryValidateModel(userRegistration))
        {
            return Ok(Result<User>.Error()
                                  .WithMessage("Validation failure")
                                  .WithErrors(ModelState.ToFluentErrors())
                     );
        }

        var errorAdditionalProp = new KeyValuePair<string, List<string>>("error", []);

        var user = await userManager.FindByNameAsync(username);

        if (user == null)
        {
            return Ok(Result<User>.Error()
                                  .WithPayload(new User
                                  {
                                      Username = userRegistration.Username,
                                      Email = userRegistration.Email
                                  })
                                  .WithMessage("User not found"));
        }

        if (userRegistration.Username != username)
        {
            return Ok(Result<User>.Error()
                                  .WithMessage("Username cannot be changed."));
        }

        user.Email = userRegistration.Email;
        user.PhoneNumber = userRegistration.PhoneNumber;

        if (!string.IsNullOrEmpty(userRegistration.Password))
        {
            var removePasswordResult = await userManager.RemovePasswordAsync(user);
            if (!removePasswordResult.Succeeded)
            {
                return Ok(Result<User>.Error()
                                      .WithPayload(new User
                                      {
                                          Username = userRegistration.Username,
                                          Email = userRegistration.Email
                                      })
                                      .WithMessage("Unable to update password"));
            }

            var changePasswordResult = await userManager.AddPasswordAsync(user, userRegistration.Password);

            if (!changePasswordResult.Succeeded)
            {
                return Ok(Result<User>.Error()
                                      .WithPayload(new User
                                      {
                                          Username = userRegistration.Username,
                                          Email = userRegistration.Email
                                      })
                                      .WithMessage("Unable to update password"));
            }
        }

        var userResult = await userManager.UpdateAsync(user);

        var result = Result<User>.Create(userResult.Succeeded)
                                 .WithPayload(new User
                                 {
                                     Username = userRegistration.Username,
                                     Email = userRegistration.Email
                                 })
                                 .WithMessageLogic(_ => userResult.Succeeded)
                                 .WithErrorMessage("User updated")
                                 .WithSuccessMessage("User update failed")
                                 .AddAdditionalProperty(new(errorAdditionalProp.Key, errorAdditionalProp.Value));
        return Ok(result);
    }

    [HttpPost("deactivate/{username}")]
    public async Task<ActionResult<Result<User>>> DeleteUser(string username, CancellationToken ct = default)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(x => x.UserName == username, ct);

        var lockoutEnableResult = await userManager.SetLockoutEnabledAsync(user!, true);
        var lockoutResult = await userManager.SetLockoutEndDateAsync(user!, DateTimeOffset.MaxValue);

        var result = Result<User>.Create(lockoutResult.Succeeded)
                                 .WithMessage("Locked user");
        return Ok(result);
    }

    [HttpPost("activate/{username}")]
    public async Task<IActionResult> ReleaseLockout(string username, CancellationToken ct = default)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(x => x.UserName == username, ct);
        await userManager.ResetAccessFailedCountAsync(user!);
        var lockoutEnableResult = await userManager.SetLockoutEnabledAsync(user!, true);
        var lockoutReleaseResult = await userManager.SetLockoutEndDateAsync(user!, null);

        var result = Result<User>.Create(lockoutReleaseResult.Succeeded)
                                 .WithMessage("Released Lockout");
        return Ok(result);
    }

    private readonly Expression<Func<ApplicationUser, User>> _userSelector = user => new User
    {
        Id = user.Id,
        Username = user.UserName,
        Email = user.Email,
        PhoneNumber = user.PhoneNumber,
        IsLocked = user.LockoutEnd != null
    };
}