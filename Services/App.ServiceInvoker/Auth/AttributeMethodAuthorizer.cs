using App.ServiceInvoker.Reflection;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace App.ServiceInvoker.Auth
{
    public class AttributeMethodAuthorizer : IMethodAuthorizer
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AttributeMethodAuthorizer(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void Authorize(MethodMetadata methodMetadata)
        {
            if (methodMetadata.RequireAuthenticated != null)
            {
                var user = _httpContextAccessor.HttpContext?.User;
                if (user == null || user.Identity == null || !user.Identity.IsAuthenticated)
                {
                    throw new UnauthorizedAccessException("User is not authenticated.");
                }
            }

            if (methodMetadata.RequireRole != null)
            {
                var rolesNeeded = methodMetadata.RequireRole.Roles;
                var user = _httpContextAccessor.HttpContext?.User;
                var userRoles = GetRolesFromClaims(user) ?? new List<string>();

                if (!userRoles.Intersect(rolesNeeded).Any())
                {
                    throw new UnauthorizedAccessException("User lacks required role(s).");
                }
            }
        }

        private List<string>? GetRolesFromClaims(System.Security.Claims.ClaimsPrincipal? principal)
        {
            if (principal == null)
            {
                return null;
            }

            var appMetaData = principal.Claims.FirstOrDefault(c => c.Type == "app_metadata")?.Value;
            if (string.IsNullOrWhiteSpace(appMetaData))
            {
                return null;
            }

            try
            {
                using var doc = JsonDocument.Parse(appMetaData);
                if (!doc.RootElement.TryGetProperty("roles", out var rolesEl) || rolesEl.ValueKind != JsonValueKind.Array)
                {
                    return null;
                }

                return rolesEl.EnumerateArray()
                    .Select(r => r.GetString())
                    .Where(r => !string.IsNullOrWhiteSpace(r))
                    .Select(r => r!)
                    .ToList();
            }
            catch (JsonException)
            {
                // If app_metadata isn't valid JSON, treat as no roles.
                return null;
            }
        }
    }
}
