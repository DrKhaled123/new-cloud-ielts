using Newtonsoft.Json.Linq;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;
using Supabase.Gotrue.Responses;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Client = Supabase.Client;
using User = Contracts.Domain.Database.User;

namespace App.Api.Accessors
{
    public class AuthAccessor : IAuthAccessor
    {
        private readonly ILogger<AuthAccessor> _logger;
        private static Client _client = null!;
        private static bool _isInitialized = false;
        private static readonly object _lock = new object();
        private readonly AppConfiguration _config;
        private string _supabaseUrl = string.Empty;

        public AuthAccessor(AppConfiguration config, ILogger<AuthAccessor> logger)
        {
            _logger = logger;
            _config = config;
            if (!_isInitialized)
            {
                lock (_lock)
                {
                    if (!_isInitialized)
                    {
                        _supabaseUrl = $"https://{config.SupabaseId}.supabase.co";
                        InitializeClient(_supabaseUrl, config.SupabaseAnonKey);
                        _isInitialized = true;
                    }
                }
            }
        }

        private static void InitializeClient(string url, string anonKey)
        {
            _client = new Client(url, anonKey, new Supabase.SupabaseOptions());
            _client.InitializeAsync().Wait();
        }

        public async Task<(User? user, string error)> SignUp(string email, string password, string? name)
        {
            try
            {
                var options = new Supabase.Gotrue.SignUpOptions();
                if (!string.IsNullOrEmpty(name))
                    options.Data = new Dictionary<string, object> { { "name", name } };
                var response = await _client.Auth.SignUp(Constants.SignUpType.Email, email, password, options);
                if (response?.User == null)
                {
                    return (null, "Sign up failed. (User null)");
                }
                var user = CreateUserFromSupabaseUser(response.User);
                return (user, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Signup failed");
                return (null, ex.Message);
            }
        }

        public async Task<(User? user, string? jwt, string? refreshToken, string error)> Login(string email, string password)
        {
            try
            {
                var response = await _client.Auth.SignInWithPassword(email,password);
                if (response?.User == null || string.IsNullOrWhiteSpace(response.AccessToken))
                {
                    return (null, null, null, "Invalid email or password.");
                }
                var user = CreateUserFromSupabaseUser(response.User);
                return (user, response.AccessToken, response.RefreshToken, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed");
                return (null, null, null, ex.Message);
            }
        }

        public async Task<bool> SendPasswordResetEmail(string email, string redirectUrl)
        {
            try
            {
                var options = new Supabase.Gotrue.ResetPasswordForEmailOptions(email)
                {
                    RedirectTo = redirectUrl
                };
                var response = await _client.Auth.ResetPasswordForEmail(options);
                // Supabase returns null on success, or error (exception)
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Password reset failed for {email}");
                return false;
            }
        }

        public async Task<bool> UpdatePassword(string userId, string newPassword)
        {
            // This method is for admin update (e.g., user already identified by id)
            try
            {
                IGotrueAdminClient<Supabase.Gotrue.User> client = _client.AdminAuth(_config.SupabaseServiceRoleKey);
                Supabase.Gotrue.User? supabaseUser = await client.GetUserById(userId);
                if (supabaseUser == null)
                {
                    return false;
                }

                AdminUserAttributes attrs = new AdminUserAttributes
                {
                    Password = newPassword
                };

                Supabase.Gotrue.User? response = await client.UpdateUserById(userId, attrs);
                if (response == null)
                {
                    throw new Exception("Failed to update password.");
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update password.");
                return false;
            }
        }

        public async Task<(string, string)> GetInviteActionLink(User user, string redirectUrl)
        {
            IGotrueAdminClient<Supabase.Gotrue.User> client = _client.AdminAuth(_config.SupabaseServiceRoleKey);
            GenerateLinkOptions options = new GenerateLinkOptions(GenerateLinkOptions.LinkType.Invite, user.Email)
            {
                // Gotrue invite link supports setting user_metadata via Data. Roles are stored in app_metadata,
                // so they are not set via invite link and should be applied via an admin update after creation.
                Data = new Dictionary<string, object>
                {
                    { "name", user.Name }
                },
                RedirectTo = redirectUrl
            }; 
            
            GenerateLinkResponse? response =  await client.GenerateLink(options);
            if (response == null)
            {
                throw new Exception("Failed to generate invite link.");
            }

            return (response.ActionLink ?? string.Empty, response.Id ?? string.Empty);
        }

        public async Task<bool> DeleteUser(string userId)
        {
            try
            {
                IGotrueAdminClient<Supabase.Gotrue.User> client = _client.AdminAuth(_config.SupabaseServiceRoleKey);
                await client.DeleteUser(userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "User deletion failed.");
                return false;
            }
        }

        private User CreateUserFromSupabaseUser(Supabase.Gotrue.User supabaseUser)
        {
            string name = string.Empty;
            if (supabaseUser.UserMetadata != null)
            {
                supabaseUser.UserMetadata.TryGetValue("name", out var nameValue);
                name = nameValue?.ToString() ?? string.Empty;
            }

            string[] roles = Array.Empty<string>();
            // Roles are sourced from app_metadata only.
            if (supabaseUser.AppMetadata != null && supabaseUser.AppMetadata.TryGetValue("roles", out var rolesValue) && rolesValue is JArray rolesArray)
            {
                roles = rolesArray.ToObject<string[]>() ?? Array.Empty<string>();
            }

            return new User
            {
                AuthId = supabaseUser.Id ?? string.Empty,
                Email = supabaseUser.Email ?? string.Empty,
                Name = name,
                Roles = roles
            };
        }

        public async Task<List<User>> GetUsersAsync()
        {
            IGotrueAdminClient<Supabase.Gotrue.User> client = _client.AdminAuth(_config.SupabaseServiceRoleKey);
            UserList<Supabase.Gotrue.User>? userList = await client.ListUsers();
            return userList?.Users?.Select(u => CreateUserFromSupabaseUser(u)).ToList() ?? new List<User>();
        }

        public async Task<User?> GetUserById(string userId)
        {
            IGotrueAdminClient<Supabase.Gotrue.User> client = _client.AdminAuth(_config.SupabaseServiceRoleKey);
            Supabase.Gotrue.User? supabaseUser = await client.GetUserById(userId);
            return supabaseUser == null ? null : CreateUserFromSupabaseUser(supabaseUser);
        }

        public async Task<bool> UpdateEmail(string userId, string newEmail)
        {
            try
            {
                IGotrueAdminClient<Supabase.Gotrue.User> client = _client.AdminAuth(_config.SupabaseServiceRoleKey);
                Supabase.Gotrue.User? supabaseUser = await client.GetUserById(userId);
                if (supabaseUser == null)
                {
                    return false;
                }
                AdminUserAttributes attrs = new AdminUserAttributes
                {
                    Email = newEmail
                };
                Supabase.Gotrue.User? response = await client.UpdateUserById(userId, attrs);
                if (response == null || response.Email != newEmail)
                {
                    throw new Exception("Failed to update email address.");
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to update email to {newEmail}.");
                return false;
            }
        }

        public async Task<bool> UpdateUserName(string userId, string newName)
        {
            try
            {
                IGotrueAdminClient<Supabase.Gotrue.User> client = _client.AdminAuth(_config.SupabaseServiceRoleKey);
                Supabase.Gotrue.User? supabaseUser = await client.GetUserById(userId);
                if (supabaseUser == null)
                {
                    return false;
                }
                
                AdminUserAttributes attrs = new AdminUserAttributes
                {
                    UserMetadata = new Dictionary<string, object> { { "name", newName } }
                };
                Supabase.Gotrue.User? response = await client.UpdateUserById(userId, attrs);
                if (response?.UserMetadata == null || response.UserMetadata["name"].ToString() != newName)
                {
                    throw new Exception("Failed to update name.");
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to update name to {newName}.");
                return false;
            }
        }

        private async Task<bool> UpdateUserRolesAsync(User user, string[] roles, Func<List<string>, List<string>> updateRolesFunc, string action)
        {
            _logger.LogInformation($"Starting {action}RolesAsync for userId: {user.AuthId} with roles: {string.Join(", ", roles)}");
            IGotrueAdminClient<Supabase.Gotrue.User> client = _client.AdminAuth(_config.SupabaseServiceRoleKey);
            Supabase.Gotrue.User? supabaseUser = await client.GetUserById(user.AuthId);
            if (supabaseUser == null)
            {
                _logger.LogError($"Supabase user with ID {user.AuthId} not found.");
                return false;
            }
            _logger.LogInformation($"Retrieved Supabase user: {supabaseUser.Email} with current roles: {string.Join(", ", user.Roles ?? Array.Empty<string>())}");
            List<string> currentRoles = user.Roles?.ToList() ?? new List<string>();
            currentRoles = updateRolesFunc(currentRoles);
            user.Roles = currentRoles.ToArray();

            // Roles are stored in app_metadata only.
            supabaseUser.AppMetadata ??= new Dictionary<string, object>();
            supabaseUser.AppMetadata["roles"] = JArray.FromObject(user.Roles);
            AdminUserAttributes attrs = new AdminUserAttributes
            {
                AppMetadata = supabaseUser.AppMetadata
            };
            Supabase.Gotrue.User? updateResponse = await client.UpdateUserById(user.AuthId, attrs);
            if (updateResponse != null)
            {
                _logger.LogInformation($"Successfully updated roles for user: {user.Email}.");
            }
            else
            {
                _logger.LogError("Failed to update Supabase user.");
            }
            _logger.LogInformation("Update Response: " + (updateResponse != null ? updateResponse.ToString() : "null"));
            return updateResponse != null;
        }

        public async Task<bool> AddRolesToUserAsync(string userId, string[] rolesToAdd)
        {
            User? user = await GetUserById(userId);
            if (user == null)
            {
                _logger.LogWarning($"User with ID {userId} not found.");
                return false;
            }
            return await UpdateUserRolesAsync(user, rolesToAdd, currentRoles =>
            {
                currentRoles.AddRange(rolesToAdd.Except(currentRoles));
                return currentRoles;
            }, "Add");
        }

        public async Task<bool> RemoveRolesFromUserAsync(string userId, string[] rolesToRemove)
        {
            User? user = await GetUserById(userId);
            if (user == null)
            {
                _logger.LogWarning($"User with ID {userId} not found.");
                return false;
            }
            return await UpdateUserRolesAsync(user, rolesToRemove, currentRoles =>
            {
                currentRoles.RemoveAll(role => rolesToRemove.Contains(role));
                return currentRoles;
            }, "Remove");
        }

        public async Task<(User? user, string? jwt, string? refreshToken, string error)> RefreshToken(string accessToken, string refreshToken)
        {
            try
            {
                var response = await _client.Auth.SetSession(accessToken, refreshToken, true);
                if (response?.User == null || response == null)
                    return (null, null, null, "Refresh token exchange failed.");
                var user = CreateUserFromSupabaseUser(response.User);
                return (user, response.AccessToken, response.RefreshToken, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Refresh token failed");
                return (null, null, null, ex.Message);
            }
        }
    }
}
