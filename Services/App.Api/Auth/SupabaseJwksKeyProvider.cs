using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;

namespace App.Api.Auth
{
    /// <summary>
    /// Fetches and caches Supabase JWKS signing keys from the well-known JWKS endpoint.
    /// </summary>
    public class SupabaseJwksKeyProvider
    {
        private const string CacheKey = "supabase-jwks-keys";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _memoryCache;
        private readonly AppConfiguration _appConfiguration;
        private readonly ILogger<SupabaseJwksKeyProvider> _logger;

        public SupabaseJwksKeyProvider(
            IHttpClientFactory httpClientFactory,
            IMemoryCache memoryCache,
            AppConfiguration appConfiguration,
            ILogger<SupabaseJwksKeyProvider> logger)
        {
            _httpClientFactory = httpClientFactory;
            _memoryCache = memoryCache;
            _appConfiguration = appConfiguration;
            _logger = logger;
        }

        public async Task<IReadOnlyCollection<SecurityKey>> GetSigningKeysAsync(string? kid, CancellationToken cancellationToken = default)
        {
            var keys = await GetAllKeysAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(kid))
            {
                return keys;
            }

            var filtered = keys.Where(k => string.Equals(k.KeyId, kid, StringComparison.OrdinalIgnoreCase)).ToList();
            return filtered.Count > 0 ? filtered : keys;
        }

        private async Task<IReadOnlyCollection<SecurityKey>> GetAllKeysAsync(CancellationToken cancellationToken)
        {
            if (_memoryCache.TryGetValue<IReadOnlyCollection<SecurityKey>>(CacheKey, out var cached) && cached != null)
            {
                return cached;
            }

            var jwksUrl = $"https://{_appConfiguration.SupabaseId}.supabase.co/auth/v1/.well-known/jwks.json";

            try
            {
                var client = _httpClientFactory.CreateClient();
                using var response = await client.GetAsync(jwksUrl, cancellationToken);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var jwks = new JsonWebKeySet(json);

                var keys = jwks.GetSigningKeys().ToList();
                // Cache for 15 minutes (per task decision)
                _memoryCache.Set(CacheKey, keys, TimeSpan.FromMinutes(15));
                return keys;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch/parse Supabase JWKS from {JwksUrl}", jwksUrl);
                // No fallback: propagate so token validation clearly fails when keys can't be retrieved.
                throw;
            }
        }
    }
}
