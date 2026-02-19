namespace App.Api.Auth
{
    /// <summary>
    /// Local-mode JWT settings used by LocalAuthAccessor (token issuer) and LocalJwtRequestAuthenticator (token validation).
    /// This is intentionally static so local mode does not depend on any secret config.
    /// </summary>
    public static class LocalJwtOptions
    {
        public const string Issuer = "ride-local";
        public const string Audience = "authenticated";

        // Dev-only signing key for local mode. Not used in production.
        // Must be sufficiently long for HMAC-SHA256.
        public const string SigningKey = "ride-local-dev-signing-key-32bytes-minimum!!";
    }
}
