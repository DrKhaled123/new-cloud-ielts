using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using App.Api.Utilities;
using App.ServiceInvoker.Auth;
using Microsoft.IdentityModel.Tokens;

namespace App.Api.Auth
{
    /// <summary>
    /// Cloud authenticator: validates Supabase access tokens via JWKS.
    /// Establishes HttpContext.User and populates UserContextService.
    /// </summary>
    public class SupabaseJwksRequestAuthenticator : IRequestAuthenticator
    {
        private readonly AppConfiguration _appConfiguration;
        private readonly SupabaseJwksKeyProvider _jwksKeyProvider;
        private readonly UserContextService _userContextService;
        private readonly ILogger<SupabaseJwksRequestAuthenticator> _logger;

        public SupabaseJwksRequestAuthenticator(
            AppConfiguration appConfiguration,
            SupabaseJwksKeyProvider jwksKeyProvider,
            UserContextService userContextService,
            ILogger<SupabaseJwksRequestAuthenticator> logger)
        {
            _appConfiguration = appConfiguration;
            _jwksKeyProvider = jwksKeyProvider;
            _userContextService = userContextService;
            _logger = logger;
        }

        public async Task AuthenticateAsync(HttpRequest request)
        {
            if (!request.Headers.TryGetValue("Authorization", out var authHeaderValues))
            {
                return;
            }

            var authHeader = authHeaderValues.ToString();
            if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var token = authHeader["Bearer ".Length..].Trim();
            if (string.IsNullOrWhiteSpace(token))
            {
                return;
            }

            ClaimsPrincipal principal;
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var parameters = BuildValidationParameters();

                principal = handler.ValidateToken(token, parameters, out _);
            }
            catch (Exception ex)
            {
                // Leave request unauthenticated; authorization will fail if the method requires it.
                _logger.LogInformation(ex, "Supabase access token validation failed.");
                return;
            }

            request.HttpContext.User = principal;
            PopulateUserContext(principal);
        }

        private TokenValidationParameters BuildValidationParameters()
        {
            var issuer = $"https://{_appConfiguration.SupabaseId}.supabase.co/auth/v1";

            return new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = "authenticated",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(2),

                ValidateIssuerSigningKey = true,
                IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
                {
                    // IssuerSigningKeyResolver is synchronous; the provider caches in memory.
                    return _jwksKeyProvider.GetSigningKeysAsync(kid).GetAwaiter().GetResult();
                }
            };
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
                    _logger.LogWarning(ex, "Failed to parse app_metadata from Supabase access token.");
                }
            }

            _userContextService.UserId = userId;
            _userContextService.UserEmail = email;
            _userContextService.UserRoles = roles;
        }
    }
}
