using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using Domain.Entities.Users;

using Infrastructure.Database;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;

using Shared.Constants;
using Shared.Extensions;
using Shared.Models.Results;
using Shared.Models.Users;
using Shared.Settings;

using WebApi.Infrastructure.Extensions;

namespace WebApi.Controllers.Users;

[Route("api/auth")]
[ApiController]
public class AuthenticationController(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IOptionsMonitor<JwtSettings> jwtOptions)
    : ControllerBase
{
    private readonly SemaphoreSlim _lockoutResetSemaphoreSlim = new SemaphoreSlim(1, 1);

    private readonly int _accessTokenExpiresIn = Convert.ToInt32(jwtOptions.CurrentValue.AccessTokenExpirationMinutes);
    private readonly int _refreshTokenExpiresIn = Convert.ToInt32(jwtOptions.CurrentValue.RefreshTokenExpirationMinutes);

    private readonly bool _extendRefreshTokenEverytime = Convert.ToBoolean(jwtOptions.CurrentValue.ExtendRefreshTokenEverytime);
    private readonly int _refreshTokenAbsoluteExpirationMinutes = Convert.ToInt32(jwtOptions.CurrentValue.RefreshTokenAbsoluteExpirationMinutes);
    private readonly bool _revokeRefreshTokenAfterAbsoluteExpiration = Convert.ToBoolean(jwtOptions.CurrentValue.RevokeRefreshTokenAfterAbsoluteExpiration);

    private readonly int _userLockoutMinutes = Convert.ToInt32(jwtOptions.CurrentValue.UserLockoutMinutes);

    // POST api/Auth/token
    [HttpPost("token")]
    public async Task<ActionResult<TokenResponse>> Token([FromForm] TokenRequest request, CancellationToken ct = default)
    {
        switch (request.GrantType)
        {
            case "password" when (IsBasicHeaderValid(request.ClientId, request.ClientSecret) || IsBasicHeaderValid(request.Authorization!) || IsBasicHeaderValid(HttpContext) && !string.IsNullOrEmpty(request.Username) && !string.IsNullOrEmpty(request.Password)):
                {
                    return await Login(request);
                }
            case "refresh_token" when !string.IsNullOrEmpty(request.RefreshToken):
                {
                    return await TokenRefresh(request);
                }
            case "password":
                {
                    return BadRequest(new
                    {
                        message = "Invalid credentials."
                    });
                }
            case "refresh_token":
                {
                    return BadRequest(new
                    {
                        message = "Invalid Refresh Token."
                    });
                }
            default:
                {
                    return BadRequest(new
                    {
                        message = "Invalid Request."
                    });
                }
        }
    }

    // Requirement alias: POST /api/auth/login
    [HttpPost("login")]
    public Task<ActionResult<TokenResponse>> LoginAlias([FromBody] TokenRequest request, CancellationToken ct = default)
    {
        // Accept JSON payload and forward as password grant
        request.GrantType ??= "password";
        return Login(request);
    }

    // Requirement alias: POST /api/auth/signup
    [HttpPost("signup")]
    public Task<IActionResult> SignupAlias([FromBody] Shared.Models.Users.User userRegistration, CancellationToken ct = default)
    {
        return Register(userRegistration, ct);
    }

    private async Task<ActionResult<TokenResponse>> Login(TokenRequest request)
    {
        string invalidCredentialsMessage = "Invalid Username or Password.";

        if (IsCredentialsValid(request))
        {
            return BadRequest(new
            {
                message = invalidCredentialsMessage
            });
        }

        var user = await GetIdentityUserAsync(request.Username!);
        if (user is null)
        {
            return BadRequest(new
            {
                message = invalidCredentialsMessage
            });
        }

        // Check for existing active sessions if single login is enabled
        // Strictly enforce single login by rejecting new login attempts
        if (jwtOptions.CurrentValue.SingleLoginEnabled)
        {
            var activeSession = user.RefreshTokens.FirstOrDefault(x => x.IsActive);
            if (activeSession != null)
            {
                string deviceInfo = !string.IsNullOrEmpty(activeSession.DeviceInfo)
                    ? $" from {activeSession.DeviceInfo}"
                    : "";

                return BadRequest(new
                {
                    message = $"You are already logged in{deviceInfo}. Please log out from the existing session before logging in again.",
                    alreadyLoggedIn = true,
                    sessionStartedAt = activeSession.CreatedAt,
                    deviceInfo = activeSession.DeviceInfo
                });
            }
        }

        var signInResult = await signInManager.PasswordSignInAsync(request.Username, request.Password!, false, false);
        if (signInResult.IsLockedOut)
        {
            var lockoutEndDate = await userManager.GetLockoutEndDateAsync(user);
            return BadRequest(new
            {
                message = $"Account is locked. Try again after {lockoutEndDate.ToString()}"
            });
        }
        if (signInResult.Succeeded)
        {
            // Only perform these operations when the user was previously locked out and the lockout period has elapsed
            var lockoutEnd = user.LockoutEnd;
            if (lockoutEnd.HasValue && lockoutEnd.Value <= DateTimeOffset.UtcNow) //if (await userManager.IsLockedOutAsync(user))
            {
                await _lockoutResetSemaphoreSlim.WaitAsync();
                try
                {
                    _ = await userManager.ResetAccessFailedCountAsync(user);

                    if (!user.LockoutEnabled)
                    {
                        _ = await userManager.SetLockoutEnabledAsync(user, true);
                    }

                    _ = await userManager.SetLockoutEndDateAsync(user, null);
                }
                finally
                {
                    _lockoutResetSemaphoreSlim.Release();
                }
            }

            string accessToken = GenerateJwtTokenAsync(user);
            var refreshTokenResult = await GenerateRefreshToken(user, TokenGenerationType.New, deviceInfo: GetDeviceInfo());

            if (refreshTokenResult.IsFailure)
            {
                return Unauthorized(new
                {
                    message = refreshTokenResult.Message
                });
            }

            return Ok(new TokenResponse
            {
                Message = "Login Successful.",
                RefreshToken = refreshTokenResult.Payload!.Token,
                AccessToken = accessToken,
                TokenType = "Bearer",
                ExpiresIn = TimeSpan.FromMinutes(_accessTokenExpiresIn).TotalSeconds,
                RefreshExpiresIn = TimeSpan.FromMinutes(_refreshTokenExpiresIn).TotalSeconds,
                Login = new TokenDetails
                {
                    AccessToken = accessToken,
                    TokenType = "Bearer"
                },
                Roles = []
            });
        }

        await userManager.AccessFailedAsync(user);

        var accessFailedCount = await userManager.GetAccessFailedCountAsync(user);

        if (accessFailedCount >= jwtOptions.CurrentValue.MaximumFailedAccessCount)
        {
            await userManager.SetLockoutEnabledAsync(user, true);

            var lockoutEndDate = DateTimeOffset.UtcNow.AddMinutes(_userLockoutMinutes);
            await userManager.SetLockoutEndDateAsync(user, lockoutEndDate);
            return BadRequest(new
            {
                message = $"Too many try with invalid credentials. Account is locked. Try again after {lockoutEndDate.ToString()}"
            });
        }

        return BadRequest(new
        {
            message = invalidCredentialsMessage + $" Your account will be locked after {jwtOptions.CurrentValue.MaximumFailedAccessCount - accessFailedCount} more tries."
        });
    }

    private static bool IsCredentialsValid(TokenRequest request)
    {

        return string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password);
    }

    private async Task<ActionResult<TokenResponse>> TokenRefresh(TokenRequest request)
    {

        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(new
            {
                message = "Invalid refresh token."
            });
        }

        var username = GetUsernameFromRefreshToken(request.RefreshToken);
        var user = await GetIdentityUserAsync(username ?? request.Username);
        if (user is null)
        {
            return BadRequest(new
            {
                message = "Invalid username."
            });
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            var lockoutEndDate = await userManager.GetLockoutEndDateAsync(user);
            return BadRequest(new
            {
                message = $"Account is locked. Try again after {lockoutEndDate.ToString()}"
            });
        }

        // Check if the current refresh token is valid and active
        var currentRefreshToken = user.RefreshTokens.FirstOrDefault(x => x.Token == request.RefreshToken);
        if (currentRefreshToken == null || !currentRefreshToken.IsActive)
        {
            // With strict single login, this generally means the token was revoked by a logout
            return Unauthorized(new
            {
                message = "Session has been invalidated. Please log in again.",
                reason = currentRefreshToken?.ReasonOfRevoke ?? "Token not found or expired"
            });
        }

        string accessToken = GenerateJwtTokenAsync(user);
        var refreshTokenResult = await GenerateRefreshToken(user, TokenGenerationType.Renew, request.RefreshToken);

        if (refreshTokenResult.IsFailure)
        {
            return Unauthorized(new
            {
                message = refreshTokenResult.Message
            });
        }

        return Ok(new TokenResponse
        {
            Message = "Token Refresh Successful.",
            RefreshToken = refreshTokenResult.Payload!.Token,
            AccessToken = accessToken,
            TokenType = "Bearer",
            ExpiresIn = TimeSpan.FromMinutes(_accessTokenExpiresIn).TotalSeconds,
            RefreshExpiresIn = TimeSpan.FromMinutes(_refreshTokenExpiresIn).TotalSeconds,
            Login = new TokenDetails
            {
                AccessToken = accessToken,
                TokenType = "Bearer"
            },
            Roles = []
        });
    }

    // POST api/Auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] User userRegistration, CancellationToken ct = default)
    {
        if (!TryValidateModel(userRegistration))
        {
            var errors = ModelState.ToFluentErrors()!
                                     .Where(kv => kv.Value != null)
                                     .ToDictionary(kv => kv.Key, kv => kv.Value!);
            return BadRequest(Result<bool>.Error()
                                          .WithMessage("Validation failed.")
                                          .WithErrors(errors));
        }

        ApplicationUser identityUser = new ApplicationUser
        {
            UserName = userRegistration.Username,
            Email = userRegistration.Email,
            PhoneNumber = userRegistration.PhoneNumber,
            DateOfBirth = userRegistration.DateOfBirth,
            LockoutEnabled = true,
        };

    var userResult = await userManager.CreateAsync(identityUser, userRegistration.Password ?? string.Empty);

        if (!userResult.Succeeded)
        {
            return BadRequest(Result<bool>.Error(UserErrors.AlreadyExists)
                                          .WithMessage("Registration failed."));
        }

        return Ok(Result<bool>.Success()
                              .WithPayload(true)
                              .WithMessage("Registered successfully."));
    }

    // POST api/Auth/logout
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
        {
            return BadRequest(new
            {
                message = "Refresh token is required."
            });
        }

        var username = GetUsernameFromRefreshToken(request.RefreshToken);
    var usernameToUse = username ?? request.Username ?? string.Empty;
    var user = await GetIdentityUserAsync(usernameToUse);

        if (user == null)
        {
            return BadRequest(new
            {
                message = "Invalid user."
            });
        }

        // Revoke the specific refresh token
        var refreshTokenResult = await GenerateRefreshToken(user, TokenGenerationType.Revoke, request.RefreshToken);

        if (refreshTokenResult.IsFailure)
        {
            return BadRequest(new
            {
                message = refreshTokenResult.Message
            });
        }

        return Ok(new
        {
            message = "Logged out successfully."
        });
    }

    // POST api/Auth/force-logout
    [HttpPost("force-logout")]
    public async Task<IActionResult> ForceLogout([FromBody] ForceLogoutRequest request, CancellationToken ct = default)
    {
        // This endpoint would typically be restricted to admin users or the account owner
        var user = await GetIdentityUserAsync(request.Username);

        if (user == null)
        {
            return BadRequest(new
            {
                message = "User not found."
            });
        }

        // Revoke all active refresh tokens for this user
        var activeTokens = user.RefreshTokens.Where(x => x.IsActive).ToList();

        foreach (var token in activeTokens)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.ReasonOfRevoke = request.Reason ?? "Administrator-initiated logout";
            token.RevokedByIp = GetIpAddress();
        }

        await context.SaveChangesAsync(ct);

        return Ok(new
        {
            message = $"Successfully logged out user from {activeTokens.Count} session(s)."
        });
    }

    // GET api/Auth/active-sessions
    [HttpGet("active-sessions")]
    public async Task<IActionResult> GetActiveSessions([FromQuery] string username, CancellationToken ct = default)
    {
        // This endpoint would typically be restricted to admin users or the account owner
        var user = await GetIdentityUserAsync(username);

        if (user == null)
        {
            return BadRequest(new
            {
                message = "User not found."
            });
        }

        var activeSessions = user.RefreshTokens
                                 .Where(x => x.IsActive)
                                 .ToList();

        return Ok(new
        {
            Username = user.UserName,
            ActiveSessions = activeSessions,
            SessionCount = activeSessions.Count
        });
    }

    #region Non Action Methods

    [NonAction]
    public Task<ApplicationUser?> GetIdentityUserAsync(string username)
    {
        if (username.Contains('@') && username.Contains('.'))
        {
            return userManager.FindByEmailAsync(username);
        }
        return userManager.FindByNameAsync(username);
    }

    private bool IsBasicHeaderValid(HttpContext httpContext)
    {
        if (!httpContext.Request.Headers.TryGetValue("Authorization", out StringValues value))
        {
            return false;
        }

        try
        {
            // decoding authToken we get decode value in 'Username:Password' format
            var authenticationHeaderValue = AuthenticationHeaderValue.Parse(value!);
            return IsBasicHeaderValid(authenticationHeaderValue.Parameter!);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private bool IsBasicHeaderValid(string basicHeader)
    {
        try
        {
            if (basicHeader.Contains(' '))
            {
                basicHeader = basicHeader.Split(' ')[1];
            }

            var bytes = Convert.FromBase64String(basicHeader);
            var decodedString = Encoding.UTF8.GetString(bytes);

            // splitting decodeAuthToken using ':'
            var splitText = decodedString.Split([':']);

            string clientId = splitText[0];
            string clientSecret = splitText[1];
            var isValidBasicHeader = IsBasicHeaderValid(clientId, clientSecret);

            return isValidBasicHeader;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private bool IsBasicHeaderValid(string? clientId, string? clientSecret)
    {
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            return false;
        }

        return clientId == jwtOptions.CurrentValue.ClientId && clientSecret == jwtOptions.CurrentValue.ClientSecret;
    }

    #region Jwt Token Helpers

    [NonAction]
    public string GenerateJwtTokenAsync(ApplicationUser user)
    {
        return GenerateJwtToken(user, jwtOptions.CurrentValue.Key, jwtOptions.CurrentValue.Issuer, jwtOptions.CurrentValue.Audience, _accessTokenExpiresIn);
    }

    private static string GenerateJwtToken(ApplicationUser user, string? signingKey, string? issuer, string? audience, int accessTokenExpiresIn)
    {
        // var userClaims = await userManager.GetClaimsAsync(user);
        List<string> roles = [];
        List<Claim> roleClaims = roles.Select(t => new Claim("roles", t)).ToList();

        IEnumerable<Claim> authClaims =
        [
            new Claim(JwtRegisteredClaimNames.Sub, user.UserName!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Id), // User.Identity.Name
            new Claim(ClaimNames.UserId, user.Id),
            new Claim(ClaimNames.UserEmail, user.Email!),
            new Claim(ClaimNames.Username, user.UserName!)
        ];

        authClaims = authClaims.Union(roleClaims);

        DateTime expireDateTime = DateTime.UtcNow.AddMinutes(accessTokenExpiresIn);

        byte[] signingKeyBytes = Encoding.UTF8.GetBytes(signingKey!);
        SymmetricSecurityKey symmetricSecurityKey = new SymmetricSecurityKey(signingKeyBytes);
        SigningCredentials signingCredentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

        JwtSecurityToken token = new JwtSecurityToken(issuer,
                                                      audience,
                                                      authClaims,
                                                      DateTime.UtcNow,
                                                      expireDateTime,
                                                      signingCredentials);
        JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(token);
    }

    #endregion Jwt Token Helpers

    #region Refresh Token Helpers

    private async Task<Result<RefreshToken>> GenerateRefreshToken(ApplicationUser user, TokenGenerationType tokenGenerationType, string? currentRefreshToken = null, string? deviceInfo = null)
    {
        // Clean up expired but not revoked tokens
        var expiredTokens = user.RefreshTokens.Where(x => x.IsExpired).ToList();
        foreach (var token in expiredTokens)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.ReasonOfRevoke = "Token expired";
        }

        // Always save changes when modifying tokens
        if (expiredTokens.Count != 0)
        {
            user.RefreshTokens.RemoveAll(x => x.IsExpired && x.CreatedAt < DateTime.UtcNow.AddDays(-jwtOptions.CurrentValue.RemoveExpiredRefreshTokenBeforeDays));
        }

        var refreshTokenFromDb = user.RefreshTokens.FirstOrDefault(x => string.IsNullOrEmpty(currentRefreshToken) || x.Token == currentRefreshToken);

        var result = tokenGenerationType switch
        {
            TokenGenerationType.New => CreateNewRefreshToken(user, jwtOptions.CurrentValue.SingleRefreshTokenPerUser ? refreshTokenFromDb : null, deviceInfo),
            TokenGenerationType.Renew => RenewRefreshToken(user, refreshTokenFromDb),
            TokenGenerationType.Revoke => RevokeRefreshToken(user, refreshTokenFromDb),
            _ => Result<RefreshToken>.Error().WithMessage("Failed to generate refresh token")
        };

        await context.SaveChangesAsync();

        return result;
    }

    private Result<RefreshToken> CreateNewRefreshToken(ApplicationUser user, RefreshToken? refreshTokenFromDb, string? deviceInfo)
    {
        if (refreshTokenFromDb == null)
        {
            var refreshToken = new RefreshToken
            {
                Token = GenerateRefreshTokenString(user.UserName!),
                Username = user.UserName!,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_refreshTokenExpiresIn),
                CreatedByIp = GetIpAddress(),
                DeviceInfo = deviceInfo
            };

            user.RefreshTokens.Add(refreshToken);

            return Result<RefreshToken>.Success().WithPayload(refreshToken);
        }
        else
        {
            refreshTokenFromDb.ExpiresAt = DateTime.UtcNow.AddMinutes(_refreshTokenExpiresIn);
            refreshTokenFromDb.DeviceInfo = deviceInfo ?? refreshTokenFromDb.DeviceInfo;
            return Result<RefreshToken>.Success().WithPayload(refreshTokenFromDb);
        }
    }

    private Result<RefreshToken> RenewRefreshToken(ApplicationUser user, RefreshToken? refreshTokenFromDb)
    {
        if (refreshTokenFromDb == null) return Result<RefreshToken>.Error().WithMessage("Refresh token not found");

        if (!refreshTokenFromDb.IsActive)
        {
            user.RefreshTokens.Remove(refreshTokenFromDb);
            return Result<RefreshToken>.Error().WithMessage("Refresh token expired");
        }

        if (_revokeRefreshTokenAfterAbsoluteExpiration && refreshTokenFromDb.CreatedAt.AddMinutes(_refreshTokenAbsoluteExpirationMinutes) <= DateTime.UtcNow)
        {
            refreshTokenFromDb.RevokedAt = DateTime.UtcNow;
            refreshTokenFromDb.ReasonOfRevoke = "Exceeded absolute lifetime";
            if (!user.RefreshTokens.Contains(refreshTokenFromDb)) user.RefreshTokens.Add(refreshTokenFromDb);

            return Result<RefreshToken>.Error().WithMessage("Refresh token expired");
        }

        if (_extendRefreshTokenEverytime)
        {
            refreshTokenFromDb.ExpiresAt = DateTime.UtcNow.AddMinutes(_refreshTokenExpiresIn);
        }

        if (!user.RefreshTokens.Contains(refreshTokenFromDb)) user.RefreshTokens.Add(refreshTokenFromDb);

        return Result<RefreshToken>.Success().WithPayload(refreshTokenFromDb);
    }

    private Result<RefreshToken> RevokeRefreshToken(ApplicationUser user, RefreshToken? refreshTokenFromDb)
    {
        switch (refreshTokenFromDb)
        {
            case null:
                return Result<RefreshToken>.Error().WithMessage("Refresh token not found");
            case
            {
                IsActive : true
            }:
                {
                    refreshTokenFromDb.RevokedAt = DateTime.UtcNow;
                    refreshTokenFromDb.ReasonOfRevoke = "Revoked by user logout";
                    refreshTokenFromDb.RevokedByIp = GetIpAddress();
                    if (!user.RefreshTokens.Contains(refreshTokenFromDb)) user.RefreshTokens.Add(refreshTokenFromDb);
                    break;
                }
        }

        return Result<RefreshToken>.Success().WithPayload(refreshTokenFromDb);
    }

    private static string GenerateRefreshTokenString(string username)
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return ToBase64String(username, Convert.ToBase64String(randomNumber));
    }

    private static string ToBase64String(string username, string refreshToken)
    {
        var combined = $"{refreshToken}::::{username}";
        return combined.StringToBase64();
    }

    private static string? GetUsernameFromRefreshToken(string? refreshToken)
    {
        try
        {
            if (string.IsNullOrEmpty(refreshToken)) return string.Empty;
            var normalString = refreshToken.Base64ToString();
            return normalString.Split("::::")[1];
        }
        catch (Exception)
        {
            return null;
        }
    }

    private string GetIpAddress()
    {
        if (Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            return forwardedFor.ToString().Split(',')[0].Trim();
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private string? GetDeviceInfo()
    {
        if (Request.Headers.TryGetValue("User-Agent", out var userAgent))
        {
            return userAgent.ToString();
        }

        return null;
    }

    #endregion Refresh Token Helpers

    #endregion Non Action Methods
}