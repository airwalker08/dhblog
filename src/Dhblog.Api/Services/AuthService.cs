using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Dhblog.Api.Authorization;
using Dhblog.Api.Settings;
using Dhblog.Database;
using Dhblog.Database.Entities;
using Dhblog.DataAccess;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Dhblog.Api.Services;

public class AuthService
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IFeatureRepository _features;
    private readonly IFeatureRoleRepository _featureRoles;
    private readonly IPasswordResetRepository _passwordResets;
    private readonly IAppSettingsProvider _settings;
    private readonly PasswordHasher<User> _hasher = new();
    private readonly JwtOptions _jwt;
    private readonly IHostEnvironment _env;

    public AuthService(
        IUserRepository users,
        IRoleRepository roles,
        IFeatureRepository features,
        IFeatureRoleRepository featureRoles,
        IPasswordResetRepository passwordResets,
        IAppSettingsProvider settings,
        IOptions<JwtOptions> jwt,
        IHostEnvironment env)
    {
        _users = users;
        _roles = roles;
        _features = features;
        _featureRoles = featureRoles;
        _passwordResets = passwordResets;
        _settings = settings;
        _jwt = jwt.Value;
        _env = env;
    }

    public async Task<LoginResult?> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        var user = await _users.GetByUsernameAsync(username, ct);
        if (user == null) return null;

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed) return null;

        var token = await GenerateTokenAsync(user, ct);
        var me = await BuildUserProfileAsync(user, ct);
        return new LoginResult(token, me);
    }

    public async Task<UserProfileDto> GetProfileAsync(string userId, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct)
            ?? throw new KeyNotFoundException("User not found");
        return await BuildUserProfileAsync(user, ct);
    }

    public async Task<ForgotPasswordResult> ForgotPasswordAsync(string email, CancellationToken ct = default)
    {
        var user = await _users.GetByEmailAsync(email, ct);
        if (user == null)
            return new ForgotPasswordResult(null, "If the email exists, a reset link was sent.");

        var ttlStr = await _settings.GetAsync(AppSettingsKeys.PasswordResetTokenTtlMinutes, ct) ?? "30";
        var ttl = int.Parse(ttlStr);
        var tokenValue = Guid.NewGuid().ToString("N");
        var token = new PasswordResetToken
        {
            TokenId = Guid.NewGuid().ToString(),
            UserId = user.UserId,
            Token = tokenValue,
            ExpiresAt = DateTime.UtcNow.AddMinutes(ttl),
            Used = false
        };
        await _passwordResets.CreateAsync(token, ct);

        var devToken = _env.IsDevelopment() ? tokenValue : null;
        return new ForgotPasswordResult(devToken, "If the email exists, a reset link was sent.");
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword, CancellationToken ct = default)
    {
        var reset = await _passwordResets.GetByTokenAsync(token, ct);
        if (reset == null || reset.Used || reset.ExpiresAt < DateTime.UtcNow)
            return false;

        var user = await _users.GetByIdAsync(reset.UserId, ct);
        if (user == null) return false;

        user.PasswordHash = _hasher.HashPassword(user, newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _users.UpdateAsync(user, ct);
        await _passwordResets.MarkUsedAsync(reset.TokenId, ct);
        return true;
    }

    private async Task<string> GenerateTokenAsync(User user, CancellationToken ct)
    {
        var expiryStr = await _settings.GetAsync(AppSettingsKeys.JwtExpiryMinutes, ct) ?? _jwt.ExpiryMinutes.ToString();
        var expiry = int.Parse(expiryStr);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.RoleId)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(
            issuer: "dhblog",
            audience: "dhblog",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiry),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private async Task<UserProfileDto> BuildUserProfileAsync(User user, CancellationToken ct)
    {
        var role = await _roles.GetByIdAsync(user.RoleId, ct);
        var featureRoles = await _featureRoles.GetByRoleIdAsync(user.RoleId, ct);
        var allFeatures = await _features.ListAsync(ct);
        var featureMap = allFeatures.ToDictionary(f => f.FeatureId);

        var features = featureRoles
            .Where(fr => featureMap.ContainsKey(fr.FeatureId))
            .Select(fr =>
            {
                var f = featureMap[fr.FeatureId];
                var perms = PermissionParser.Parse(fr.Permissions);
                return new FeatureAccessDto(
                    f.Code, f.Name, f.NavPath, f.SortOrder,
                    PermissionParser.HasRead(perms),
                    PermissionParser.HasWrite(perms),
                    string.IsNullOrEmpty(f.ParentFeatureId) ? null : featureMap.GetValueOrDefault(f.ParentFeatureId)?.Code);
            })
            .OrderBy(f => f.SortOrder)
            .ToList();

        return new UserProfileDto(
            user.UserId, user.Username, user.Email, user.FirstName, user.LastName,
            user.Locale, user.TimeZone, user.Language,
            role?.Name ?? user.RoleId, features);
    }
}

public record LoginResult(string Token, UserProfileDto User);
public record ForgotPasswordResult(string? DevToken, string Message);
public record UserProfileDto(
    string UserId, string Username, string Email, string FirstName, string LastName,
    string Locale, string TimeZone, string Language, string RoleName,
    IReadOnlyList<FeatureAccessDto> Features);
public record FeatureAccessDto(
    string Code, string Name, string NavPath, int SortOrder, bool CanRead, bool CanWrite, string? ParentCode);
