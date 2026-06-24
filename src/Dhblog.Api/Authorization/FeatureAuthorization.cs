using System.Security.Claims;
using System.Text;
using Dhblog.Database;
using Dhblog.Database.Entities;
using Dhblog.DataAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Dhblog.Api.Authorization;

public class FeatureRequirement : IAuthorizationRequirement
{
    public string FeatureCode { get; }
    public bool RequireWrite { get; }

    public FeatureRequirement(string featureCode, bool requireWrite = false)
    {
        FeatureCode = featureCode;
        RequireWrite = requireWrite;
    }
}

public class FeatureAuthorizationHandler : AuthorizationHandler<FeatureRequirement>
{
    private readonly IFeatureRepository _features;
    private readonly IFeatureRoleRepository _featureRoles;
    private readonly IUserRepository _users;

    public FeatureAuthorizationHandler(
        IFeatureRepository features,
        IFeatureRoleRepository featureRoles,
        IUserRepository users)
    {
        _features = features;
        _featureRoles = featureRoles;
        _users = users;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        FeatureRequirement requirement)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return;

        var user = await _users.GetByIdAsync(userId);
        if (user == null) return;

        var feature = await _features.GetByCodeAsync(requirement.FeatureCode);
        if (feature == null) return;

        var featureRoles = await _featureRoles.GetByRoleIdAsync(user.RoleId);
        var fr = featureRoles.FirstOrDefault(x => x.FeatureId == feature.FeatureId);
        if (fr == null) return;

        var perms = PermissionParser.Parse(fr.Permissions);
        if (!PermissionParser.HasRead(perms)) return;
        if (requirement.RequireWrite && !PermissionParser.HasWrite(perms)) return;

        context.Succeed(requirement);
    }
}

public class JwtOptions
{
    public string Secret { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 60;
}

public static class ClaimsPrincipalExtensions
{
    public static string? GetUserId(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier);
}
