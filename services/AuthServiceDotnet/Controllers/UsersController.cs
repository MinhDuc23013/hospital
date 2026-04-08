using System.Text.Json;
using AuthServiceDotnet.Application.DTOs;
using AuthServiceDotnet.Infrastructure.Keycloak;
using Microsoft.AspNetCore.Mvc;

namespace AuthServiceDotnet.Controllers;

/// <summary>
/// CRUD operations for Keycloak users.
/// All endpoints require admin role except seed and change-password.
/// Gateway handles JWT validation — this service trusts forwarded headers.
/// </summary>
[ApiController]
[Route("api/auth/users")]
public class UsersController : ControllerBase
{
    private readonly KeycloakAdminClient _keycloak;
    private readonly ILogger<UsersController> _logger;

    public UsersController(KeycloakAdminClient keycloak, ILogger<UsersController> logger)
    {
        _keycloak = keycloak;
        _logger = logger;
    }

    /// <summary>Seed the first admin account. Anonymous — only works once.</summary>
    [HttpPost("/api/auth/seed")]
    public async Task<IActionResult> SeedAdmin([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var existing = await _keycloak.FindUserByEmailAsync(request.Email, ct);
        if (existing != null)
            return Conflict(new { error = "Admin account already exists" });

        var userId = await _keycloak.CreateUserAsync(request.Email, request.Password, request.FirstName, request.LastName, ct);
        await _keycloak.AssignRoleAsync(userId, "admin", ct);

        _logger.LogWarning("SEED: First admin account created: {Email}", request.Email);
        return Created("", new { keycloakUserId = userId, email = request.Email, role = "admin" });
    }

    /// <summary>Create a new user with specified role.</summary>
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var existing = await _keycloak.FindUserByEmailAsync(request.Email, ct);
        if (existing != null)
            return Conflict(new { error = "Email already registered" });

        var userId = await _keycloak.CreateUserAsync(request.Email, request.Password, request.FirstName, request.LastName, ct);
        await _keycloak.AssignRoleAsync(userId, request.Role, ct);

        _logger.LogInformation("User created: {Email} role={Role}", request.Email, request.Role);
        return Created($"/api/auth/users/{userId}", new
        {
            keycloakUserId = userId,
            email = request.Email,
            firstName = request.FirstName,
            lastName = request.LastName,
            role = request.Role
        });
    }

    /// <summary>List users with optional search and pagination.</summary>
    [HttpGet]
    public async Task<IActionResult> ListUsers(
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var first = (page - 1) * pageSize;
        var users = await _keycloak.ListUsersAsync(search, first, pageSize, ct);

        var result = users.Select(u => new
        {
            id = u.GetProperty("id").GetString(),
            email = u.TryGetProperty("email", out var e) ? e.GetString() : null,
            firstName = u.TryGetProperty("firstName", out var f) ? f.GetString() : null,
            lastName = u.TryGetProperty("lastName", out var l) ? l.GetString() : null,
            enabled = u.TryGetProperty("enabled", out var en) && en.GetBoolean()
        });

        return Ok(new { data = result, page, pageSize });
    }

    /// <summary>Get a user by Keycloak ID.</summary>
    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUser(string userId, CancellationToken ct)
    {
        var user = await _keycloak.GetUserByIdAsync(userId, ct);
        if (user == null) return NotFound(new { error = "User not found" });

        var roles = await _keycloak.GetUserRolesAsync(userId, ct);
        var roleNames = roles.Select(r => r.GetProperty("name").GetString()).ToArray();

        return Ok(new
        {
            id = user.Value.GetProperty("id").GetString(),
            email = user.Value.TryGetProperty("email", out var e) ? e.GetString() : null,
            firstName = user.Value.TryGetProperty("firstName", out var f) ? f.GetString() : null,
            lastName = user.Value.TryGetProperty("lastName", out var l) ? l.GetString() : null,
            enabled = user.Value.TryGetProperty("enabled", out var en) && en.GetBoolean(),
            roles = roleNames
        });
    }

    /// <summary>Update user details.</summary>
    [HttpPut("{userId}")]
    public async Task<IActionResult> UpdateUser(string userId, [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        var user = await _keycloak.GetUserByIdAsync(userId, ct);
        if (user == null) return NotFound(new { error = "User not found" });

        await _keycloak.UpdateUserAsync(userId, request.FirstName, request.LastName, request.Enabled, ct);
        return Ok(new { message = "User updated" });
    }

    /// <summary>Delete a user.</summary>
    [HttpDelete("{userId}")]
    public async Task<IActionResult> DeleteUser(string userId, CancellationToken ct)
    {
        var user = await _keycloak.GetUserByIdAsync(userId, ct);
        if (user == null) return NotFound(new { error = "User not found" });

        await _keycloak.DeleteUserAsync(userId, ct);
        return Ok(new { message = "User deleted" });
    }

    /// <summary>Assign a role to a user.</summary>
    [HttpPost("{userId}/roles")]
    public async Task<IActionResult> AssignRole(string userId, [FromBody] AssignRoleRequest request, CancellationToken ct)
    {
        await _keycloak.AssignRoleAsync(userId, request.Role, ct);
        return Ok(new { message = $"Role '{request.Role}' assigned" });
    }

    /// <summary>Remove a role from a user.</summary>
    [HttpDelete("{userId}/roles/{roleName}")]
    public async Task<IActionResult> RemoveRole(string userId, string roleName, CancellationToken ct)
    {
        await _keycloak.RemoveRoleAsync(userId, roleName, ct);
        return Ok(new { message = $"Role '{roleName}' removed" });
    }

    /// <summary>Reset a user's password (admin action).</summary>
    [HttpPut("{userId}/reset-password")]
    public async Task<IActionResult> ResetPassword(string userId, [FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        var user = await _keycloak.GetUserByIdAsync(userId, ct);
        if (user == null) return NotFound(new { error = "User not found" });

        await _keycloak.ChangePasswordAsync(userId, request.NewPassword, ct);
        return Ok(new { message = "Password reset" });
    }

    /// <summary>Change own password — userId from forwarded header.</summary>
    [HttpPut("/api/auth/change-password")]
    public async Task<IActionResult> ChangeOwnPassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var userId = Request.Headers["X-User-Id"].FirstOrDefault();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { error = "Missing X-User-Id header" });

        await _keycloak.ChangePasswordAsync(userId, request.NewPassword, ct);
        return Ok(new { message = "Password changed" });
    }
}
