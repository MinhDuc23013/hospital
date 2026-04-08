namespace AuthServiceDotnet.Application.DTOs;

public record CreateUserRequest(string Email, string Password, string FirstName, string LastName, string Role);
public record UpdateUserRequest(string? FirstName, string? LastName, bool? Enabled);
public record ChangePasswordRequest(string NewPassword);
public record ResetPasswordRequest(string NewPassword);
public record AssignRoleRequest(string Role);

public record UserResponse(string Id, string Email, string FirstName, string LastName, bool Enabled, string[] Roles);
