using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace LinkittyDo.Api.Services;

/// <summary>
/// Transforms claims by adding role claims from the database for policy-based authorization.
/// </summary>
public class RoleClaimsTransformation : IClaimsTransformation
{
    private readonly IRoleService _roleService;

    public RoleClaimsTransformation(IRoleService roleService)
    {
        _roleService = roleService;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = principal.Identity as ClaimsIdentity;
        if (identity == null || !identity.IsAuthenticated)
            return principal;

        var userId = identity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return principal;

        var roles = await _roleService.GetUserRolesAsync(userId);
        foreach (var role in roles)
        {
            if (!identity.HasClaim(ClaimTypes.Role, role))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, role));
            }
        }

        return principal;
    }
}
