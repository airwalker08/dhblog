using Dhblog.Database;
using Dhblog.Database.Entities;
using Dhblog.DataAccess;
using Microsoft.AspNetCore.Identity;

namespace Dhblog.Api.Services;

public class AdminService
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly ITopicRepository _topics;
    private readonly PasswordHasher<User> _hasher = new();

    public AdminService(IUserRepository users, IRoleRepository roles, ITopicRepository topics)
    {
        _users = users;
        _roles = roles;
        _topics = topics;
    }

    public async Task<IReadOnlyList<AdminUserDto>> ListUsersAsync(CancellationToken ct = default)
    {
        var users = await _users.ListAsync(ct);
        var roleMap = (await _roles.ListAsync(ct)).ToDictionary(r => r.RoleId);
        return users
            .OrderBy(u => u.Username)
            .Select(u => ToUserDto(u, roleMap.GetValueOrDefault(u.RoleId)))
            .ToList();
    }

    public async Task<AdminUserDto?> GetUserAsync(string userId, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user == null) return null;
        var role = await _roles.GetByIdAsync(user.RoleId, ct);
        return ToUserDto(user, role);
    }

    public async Task<AdminUserDto> CreateUserAsync(CreateAdminUserRequest request, CancellationToken ct = default)
    {
        if (await _users.GetByUsernameAsync(request.Username.Trim(), ct) != null)
            throw new InvalidOperationException("Username already exists.");
        if (await _users.GetByEmailAsync(request.Email.Trim(), ct) != null)
            throw new InvalidOperationException("Email already exists.");
        if (await _roles.GetByIdAsync(request.RoleId, ct) == null)
            throw new InvalidOperationException("Role not found.");

        var user = new User
        {
            UserId = Guid.NewGuid().ToString(),
            Username = request.Username.Trim(),
            Email = request.Email.Trim(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Locale = string.IsNullOrWhiteSpace(request.Locale) ? "en-US" : request.Locale.Trim(),
            TimeZone = string.IsNullOrWhiteSpace(request.TimeZone) ? "UTC" : request.TimeZone.Trim(),
            Language = string.IsNullOrWhiteSpace(request.Language) ? "en" : request.Language.Trim(),
            RoleId = request.RoleId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        user.PasswordHash = _hasher.HashPassword(user, request.Password);
        await _users.CreateAsync(user, ct);
        var role = await _roles.GetByIdAsync(user.RoleId, ct);
        return ToUserDto(user, role);
    }

    public async Task<AdminUserDto?> UpdateUserAsync(string userId, UpdateAdminUserRequest request, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user == null) return null;

        var existingUsername = await _users.GetByUsernameAsync(request.Username.Trim(), ct);
        if (existingUsername != null && existingUsername.UserId != userId)
            throw new InvalidOperationException("Username already exists.");
        var existingEmail = await _users.GetByEmailAsync(request.Email.Trim(), ct);
        if (existingEmail != null && existingEmail.UserId != userId)
            throw new InvalidOperationException("Email already exists.");
        if (await _roles.GetByIdAsync(request.RoleId, ct) == null)
            throw new InvalidOperationException("Role not found.");

        user.Username = request.Username.Trim();
        user.Email = request.Email.Trim();
        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Locale = string.IsNullOrWhiteSpace(request.Locale) ? "en-US" : request.Locale.Trim();
        user.TimeZone = string.IsNullOrWhiteSpace(request.TimeZone) ? "UTC" : request.TimeZone.Trim();
        user.Language = string.IsNullOrWhiteSpace(request.Language) ? "en" : request.Language.Trim();
        user.RoleId = request.RoleId;
        user.UpdatedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(request.Password))
            user.PasswordHash = _hasher.HashPassword(user, request.Password);

        await _users.UpdateAsync(user, ct);
        var role = await _roles.GetByIdAsync(user.RoleId, ct);
        return ToUserDto(user, role);
    }

    public async Task<bool> DeleteUserAsync(string userId, string currentUserId, CancellationToken ct = default)
    {
        if (userId == currentUserId)
            throw new InvalidOperationException("You cannot delete your own account.");
        var user = await _users.GetByIdAsync(userId, ct);
        if (user == null) return false;
        await _users.DeleteAsync(userId, ct);
        return true;
    }

    public async Task<IReadOnlyList<AdminRoleDto>> ListRolesAsync(CancellationToken ct = default)
    {
        var roles = await _roles.ListAsync(ct);
        return roles.OrderBy(r => r.Name).Select(ToRoleDto).ToList();
    }

    public async Task<AdminRoleDto?> GetRoleAsync(string roleId, CancellationToken ct = default)
    {
        var role = await _roles.GetByIdAsync(roleId, ct);
        return role == null ? null : ToRoleDto(role);
    }

    public async Task<AdminRoleDto> CreateRoleAsync(CreateAdminRoleRequest request, CancellationToken ct = default)
    {
        var role = new Role
        {
            RoleId = Guid.NewGuid().ToString(),
            Name = request.Name.Trim(),
            Description = request.Description.Trim()
        };
        await _roles.CreateAsync(role, ct);
        return ToRoleDto(role);
    }

    public async Task<AdminRoleDto?> UpdateRoleAsync(string roleId, UpdateAdminRoleRequest request, CancellationToken ct = default)
    {
        var role = await _roles.GetByIdAsync(roleId, ct);
        if (role == null) return null;
        role.Name = request.Name.Trim();
        role.Description = request.Description.Trim();
        await _roles.UpdateAsync(role, ct);
        return ToRoleDto(role);
    }

    public async Task<bool> DeleteRoleAsync(string roleId, CancellationToken ct = default)
    {
        if (roleId is SeedIds.RoleAdministrator or SeedIds.RoleStandardUser or SeedIds.RoleReadOnly)
            throw new InvalidOperationException("Built-in roles cannot be deleted.");

        var role = await _roles.GetByIdAsync(roleId, ct);
        if (role == null) return false;

        var users = await _users.ListAsync(ct);
        if (users.Any(u => u.RoleId == roleId))
            throw new InvalidOperationException("Role is assigned to one or more users.");

        await _roles.DeleteAsync(roleId, ct);
        return true;
    }

    public async Task<IReadOnlyList<AdminTopicDto>> ListTopicsAsync(CancellationToken ct = default)
    {
        var topics = await _topics.ListAsync(ct);
        return topics.Select(ToTopicDto).ToList();
    }

    public async Task<AdminTopicDto?> GetTopicAsync(string topicId, CancellationToken ct = default)
    {
        var topic = await _topics.GetByIdAsync(topicId, ct);
        return topic == null ? null : ToTopicDto(topic);
    }

    public async Task<AdminTopicDto> CreateTopicAsync(string userId, CreateAdminTopicRequest request, CancellationToken ct = default)
    {
        var displayText = request.DisplayText.Trim();
        var normalized = TopicNormalizer.Normalize(displayText);
        if (await _topics.GetByNormalizedKeyAsync(normalized, ct) != null)
            throw new InvalidOperationException("A topic with this name already exists.");

        var topic = new Topic
        {
            TopicId = Guid.NewGuid().ToString(),
            DisplayText = displayText,
            NormalizedKey = normalized,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };
        await _topics.CreateAsync(topic, ct);
        return ToTopicDto(topic);
    }

    public async Task<AdminTopicDto?> UpdateTopicAsync(string topicId, UpdateAdminTopicRequest request, CancellationToken ct = default)
    {
        var topic = await _topics.GetByIdAsync(topicId, ct);
        if (topic == null) return null;

        var displayText = request.DisplayText.Trim();
        var normalized = TopicNormalizer.Normalize(displayText);
        var existing = await _topics.GetByNormalizedKeyAsync(normalized, ct);
        if (existing != null && existing.TopicId != topicId)
            throw new InvalidOperationException("A topic with this name already exists.");

        topic.DisplayText = displayText;
        topic.NormalizedKey = normalized;
        await _topics.UpdateAsync(topic, ct);
        return ToTopicDto(topic);
    }

    public async Task<bool> DeleteTopicAsync(string topicId, CancellationToken ct = default)
    {
        var topic = await _topics.GetByIdAsync(topicId, ct);
        if (topic == null) return false;
        await _topics.DeleteEntryLinksByTopicIdAsync(topicId, ct);
        await _topics.DeleteAsync(topicId, ct);
        return true;
    }

    private static AdminUserDto ToUserDto(User user, Role? role) => new(
        user.UserId, user.Username, user.Email, user.FirstName, user.LastName,
        user.RoleId, role?.Name ?? user.RoleId, user.Locale, user.TimeZone, user.Language);

    private static AdminRoleDto ToRoleDto(Role role) =>
        new(role.RoleId, role.Name, role.Description);

    private static AdminTopicDto ToTopicDto(Topic topic) =>
        new(topic.TopicId, topic.DisplayText, topic.NormalizedKey, topic.CreatedByUserId, topic.CreatedAt);
}

public record AdminUserDto(
    string UserId, string Username, string Email, string FirstName, string LastName,
    string RoleId, string RoleName, string Locale, string TimeZone, string Language);
public record CreateAdminUserRequest(
    string Username, string Email, string Password, string FirstName, string LastName,
    string RoleId, string Locale, string TimeZone, string Language);
public record UpdateAdminUserRequest(
    string Username, string Email, string? Password, string FirstName, string LastName,
    string RoleId, string Locale, string TimeZone, string Language);
public record AdminRoleDto(string RoleId, string Name, string Description);
public record CreateAdminRoleRequest(string Name, string Description);
public record UpdateAdminRoleRequest(string Name, string Description);
public record AdminTopicDto(
    string TopicId, string DisplayText, string NormalizedKey, string CreatedByUserId, DateTime CreatedAt);
public record CreateAdminTopicRequest(string DisplayText);
public record UpdateAdminTopicRequest(string DisplayText);
