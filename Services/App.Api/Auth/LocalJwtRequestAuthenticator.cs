using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using App.Api.Utilities;
using App.ServiceInvoker.Auth;
using Microsoft.IdentityModel.Tokens;

namespace App.Api.Auth
{
    /// <summary>
    /// Local authenticator: validates access tokens signed with the local symmetric key.
    /// Intended for local-mode StarterKit usage with LocalAuthAccessor.
    /// </summary>
    public class LocalJwtRequestAuthenticator : IRequestAuthenticator
    {
        private readonly UserContextService _userContextService;
        private readonly ILogger<LocalJwtRequestAuthenticator> _logger;

        public LocalJwtRequestAuthenticator(
            UserContextService userContextService,
            ILogger<LocalJwtRequestAuthenticator> logger)
        {
            _userContextService = userContextService;
            _logger = logger;
        }

        public Task AuthenticateAsync(HttpRequest request)
        {
            if (!request.Headers.TryGetValue("Authorization", out var authHeaderValues))
            {
                return Task.CompletedTask;
            }

            var authHeader = authHeaderValues.ToString();
            if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return Task.CompletedTask;
            }

            var token = authHeader["Bearer ".Length..].Trim();
            if (string.IsNullOrWhiteSpace(token))
            {
                return Task.CompletedTask;
            }

            ClaimsPrincipal principal;
            try
            {
                var handler = new JwtSecurityTokenHandler();

                var parameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = LocalJwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = LocalJwtOptions.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(2),
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(LocalJwtOptions.SigningKey))
                };

                principal = handler.ValidateToken(token, parameters, out _);
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "Local access token validation failed.");
                return Task.CompletedTask;
            }

            request.HttpContext.User = principal;
            PopulateUserContext(principal);
            return Task.CompletedTask;
        }

        private void PopulateUserContext(ClaimsPrincipal principal)
        {
            var userId = principal.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type == "uid")?.Value;
            var email = principal.Claims.FirstOrDefault(c => c.Type == "email")?.Value;

            var appMetaData = principal.Claims.FirstOrDefault(c => c.Type == "app_metadata")?.Value;
            var roles = new List<string>();

            if (!string.IsNullOrWhiteSpace(appMetaData))
            {
                try
                {
                    using var doc = JsonDocument.Parse(appMetaData);
                    if (doc.RootElement.TryGetProperty("roles", out var rolesEl) && rolesEl.ValueKind == JsonValueKind.Array)
                    {
                        roles = rolesEl.EnumerateArray()
                            .Select(r => r.GetString())
                            .Where(r => !string.IsNullOrWhiteSpace(r))
                            .Select(r => r!)
                            .ToList();
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse app_metadata from local access token.");
                }
            }

            _userContextService.UserId = userId;
            _userContextService.UserEmail = email;
            _userContextService.UserRoles = roles;
        }
    }
}
