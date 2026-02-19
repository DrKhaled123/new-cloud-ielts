using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using App.Api.Auth;
using Contracts.Domain.Database;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using App.Api.Utilities;

namespace App.Api.Accessors
{
    public class LocalAuthAccessor : IAuthAccessor
    {
        private readonly string _userFilePath;
        private readonly ILogger<LocalAuthAccessor> _logger;
        private readonly AppConfiguration _appConfiguration;
        private readonly string _mockRefreshToken = "local-dev-static-refresh-token";
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };

        public LocalAuthAccessor(AppConfiguration config, ILogger<LocalAuthAccessor> logger)
        {
            var rootDir = LocalPathHelper.GetAppResourcePath(config.AppName);
            Directory.CreateDirectory(rootDir);
            _userFilePath = Path.Combine(rootDir, "Users.json");
            _logger = logger;
            _appConfiguration = config;
        }

        private async Task<List<User>> LoadUsersAsync()
        {
            if (!File.Exists(_userFilePath))
                return new List<User>();
            await using var stream = File.OpenRead(_userFilePath);
            return await JsonSerializer.DeserializeAsync<List<User>>(stream, JsonOptions) ?? new List<User>();
        }

        private async Task SaveUsersAsync(List<User> users)
        {
            await using var stream = File.Create(_userFilePath);
            await JsonSerializer.SerializeAsync(stream, users, JsonOptions);
        }

        private string GenerateJwt(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(LocalJwtOptions.SigningKey);
            var appMetadata = new { roles = user.Roles };
            string appMetaJson = JsonSerializer.Serialize(appMetadata);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.AuthId ?? ""),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                new Claim("app_metadata", appMetaJson)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(12),
                Issuer = LocalJwtOptions.Issuer,
                Audience = LocalJwtOptions.Audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public Task<(string, string)> GetInviteActionLink(User user, string redirectUrl)
        {
            var userId = string.IsNullOrWhiteSpace(user.AuthId) ? Guid.NewGuid().ToString() : user.AuthId;
            var link = $"file-invite://invite?userId={userId}&redirect={Uri.EscapeDataString(redirectUrl)}";
            return Task.FromResult((link, userId));
        }

        public async Task<bool> DeleteUser(string userId)
        {
            var users = await LoadUsersAsync();
            var beforeCount = users.Count;
            users.RemoveAll(u => u.AuthId == userId);
            await SaveUsersAsync(users);
            return users.Count < beforeCount;
        }

        public async Task<List<User>> GetUsersAsync()
        {
            return await LoadUsersAsync();
        }

        public async Task<User?> GetUserById(string userId)
        {
            return (await LoadUsersAsync()).FirstOrDefault(u => u.AuthId == userId);
        }

        public async Task<bool> UpdateEmail(string userId, string newEmail)
        {
            var users = await LoadUsersAsync();
            var user = users.FirstOrDefault(u => u.AuthId == userId);
            if (user == null) return false;
            user.Email = newEmail;
            await SaveUsersAsync(users);
            return true;
        }

        public async Task<bool> UpdatePassword(string userId, string newPassword)
        {
            // For local/mock: just log and always return true (not secure, for dev only)
            _logger.LogInformation($"Mock change password for {userId} to '{newPassword}' (no-op in dev mode)");
            return await Task.FromResult(true);
        }

        public async Task<bool> UpdateUserName(string userId, string newName)
        {
            var users = await LoadUsersAsync();
            var user = users.FirstOrDefault(u => u.AuthId == userId);
            if (user == null) return false;
            user.Name = newName;
            await SaveUsersAsync(users);
            return true;
        }

        public async Task<bool> AddRolesToUserAsync(string userId, string[] rolesToAdd)
        {
            var users = await LoadUsersAsync();
            var user = users.FirstOrDefault(u => u.AuthId == userId);
            if (user == null) return false;
            user.Roles = user.Roles.Union(rolesToAdd).Distinct().ToArray();
            await SaveUsersAsync(users);
            return true;
        }

        public async Task<bool> RemoveRolesFromUserAsync(string userId, string[] rolesToRemove)
        {
            var users = await LoadUsersAsync();
            var user = users.FirstOrDefault(u => u.AuthId == userId);
            if (user == null) return false;
            user.Roles = user.Roles.Except(rolesToRemove).ToArray();
            await SaveUsersAsync(users);
            return true;
        }

        public async Task<(User? user, string error)> SignUp(string email, string password, string? name)
        {
            var users = await LoadUsersAsync();
            var already = users.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
            if (already != null)
                return (null, "User already exists");
            var user = new User
            {
                AuthId = Guid.NewGuid().ToString(),
                Email = email,
                Name = string.IsNullOrEmpty(name) ? email : name,
                Roles = new[] { "user" }
            };
            users.Add(user);
            await SaveUsersAsync(users);
            return (user, string.Empty);
        }

        public async Task<(User? user, string? jwt, string? refreshToken, string error)> Login(string email, string password)
        {
            var users = await LoadUsersAsync();
            var user = users.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
            if (user == null)
                return (null, null, null, "Invalid email or user not found.");
            string jwt = GenerateJwt(user);
            return (user, jwt, _mockRefreshToken, string.Empty);
        }

        // Password reset: mock/no-op
        public Task<bool> SendPasswordResetEmail(string email, string redirectUrl)
        {
            _logger.LogInformation($"Mock password reset email (not sent) to {email}, redirect: {redirectUrl}");
            return Task.FromResult(true);
        }

        public async Task<(User? user, string? jwt, string? refreshToken, string error)> RefreshToken(string accessToken, string refreshToken)
        {
            if (refreshToken != _mockRefreshToken)
            {
                return (null, null, null, "Invalid refresh token");
            }

            var userId = TryGetUserIdFromAccessToken(accessToken);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return (null, null, null, "Invalid access token");
            }

            var users = await LoadUsersAsync();
            var user = users.FirstOrDefault(u => u.AuthId == userId);
            if (user == null)
            {
                return (null, null, null, "User not found in dev");
            }
            var jwt = GenerateJwt(user);
            return (user, jwt, _mockRefreshToken, string.Empty);
        }

        private string? TryGetUserIdFromAccessToken(string accessToken)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = LocalJwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = LocalJwtOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(LocalJwtOptions.SigningKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };

                ClaimsPrincipal principal = tokenHandler.ValidateToken(accessToken, validationParameters, out _);
                var userId = principal.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
                return userId;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to validate/parse access token during refresh (dev auth).");
                return null;
            }
        }
    }
}
