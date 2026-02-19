using App.Api.Accessors;
using App.Api.Utilities;
using App.ServiceInvoker.Attributes;
using App.ServiceInvoker.Interfaces;
using Contracts.Dto;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace App.Api.Managers
{
    public class AuthManager : IManagerService
    {
        private readonly ILogger<AuthManager> _logger;
        private readonly IConfiguration _config;
        private readonly IAuthAccessor _authAccessor;
        private readonly UserContextService _userContextService;

        public AuthManager(ILogger<AuthManager> logger, IConfiguration config, IAuthAccessor authAccessor,
            UserContextService userContextService)
        {
            _logger = logger;
            _config = config;
            _authAccessor = authAccessor;
            _userContextService = userContextService;
        }

        public async Task<LoginResponseDto> Login(LoginRequestDto request)
        {
            var (user, jwt, refreshToken, error) = await _authAccessor.Login(request.Email, request.Password);
            if (user == null || string.IsNullOrEmpty(jwt) || string.IsNullOrEmpty(refreshToken))
            {
                return new LoginResponseDto
                {
                    Success = false,
                    Message = string.IsNullOrWhiteSpace(error) ? "Invalid email or password." : error,
                    JwtToken = null,
                    RefreshToken = null,
                    User = null
                };
            }

            return new LoginResponseDto
            {
                Success = true,
                JwtToken = jwt,
                RefreshToken = refreshToken,
                User = new UserDto
                {
                    AuthId = user.AuthId,
                    Email = user.Email,
                    Name = user.Name,
                    Roles = user.Roles
                },
                Message = "Login successful."
            };
        }

        public async Task<SignUpResponseDto> SignUp(SignUpRequestDto request)
        {
            var (user, error) = await _authAccessor.SignUp(request.Email, request.Password, request.Name);
            if (user == null)
            {
                return new SignUpResponseDto
                {
                    Success = false,
                    Message = string.IsNullOrWhiteSpace(error) ? "Sign up failed." : error,
                    User = null
                };
            }

            return new SignUpResponseDto
            {
                Success = true,
                User = new UserDto
                {
                    AuthId = user.AuthId,
                    Email = user.Email,
                    Name = user.Name,
                    Roles = user.Roles
                },
                Message = "Sign up successful."
            };
        }

        public async Task<OperationResultDto> SendPasswordResetEmail(SendPasswordResetRequestDto request)
        {
            var result = await _authAccessor.SendPasswordResetEmail(request.Email, request.RedirectUrl ?? string.Empty);
            return new OperationResultDto
            {
                Success = result,
                Message = result ? "Password reset email sent." : "Failed to send password reset email."
            };
        }

        [RequireAuthenticated]
        public async Task<OperationResultDto> ChangePassword(ChangePasswordRequestDto request)
        {
            var userId = _userContextService.UserId;
            var result = await _authAccessor.UpdatePassword(userId, request.NewPassword);
            return new OperationResultDto
            {
                Success = result,
                Message = result ? "Password change successful." : "Failed to change password."
            };
        }

        [RequireAuthenticated]
        public async Task<UpdateUserEmailResponseDto> UpdateUserEmail(UpdateUserEmailDto request)
        {
            var userId = _userContextService.UserId;
            var success = await _authAccessor.UpdateEmail(userId, request.NewEmail);
            return new UpdateUserEmailResponseDto { Success = success, Message = success ? "Email updated successfully." : "Failed to update email." };
        }

        [RequireAuthenticated]
        public async Task<UpdateUserPasswordResponseDto> UpdateUserPassword(UpdateUserPasswordDto request)
        {
            var userId = _userContextService.UserId;
            var success = await _authAccessor.UpdatePassword(userId, request.NewPassword);
            return new UpdateUserPasswordResponseDto { Success = success, Message = success ? "Password updated successfully." : "Failed to update password." };
        }

        [RequireAuthenticated]
        public async Task<UpdateUserNameResponseDto> UpdateUserName(UpdateUserNameDto request)
        {
            var userId = _userContextService.UserId;
            var success = await _authAccessor.UpdateUserName(userId, request.NewName);
            return new UpdateUserNameResponseDto { Success = success, Message = success ? "Name updated successfully." : "Failed to update name." };
        }

        public async Task<RefreshTokenResponseDto> RefreshToken(RefreshTokenRequestDto request)
        {
            var (user, jwt, refreshToken, error) = await _authAccessor.RefreshToken(request.AccessToken, request.RefreshToken);
            return new RefreshTokenResponseDto
            {
                Success = !string.IsNullOrWhiteSpace(jwt) && !string.IsNullOrWhiteSpace(refreshToken),
                JwtToken = jwt,
                RefreshToken = refreshToken,
                User = user == null
                    ? null
                    : new UserDto
                    {
                        AuthId = user.AuthId,
                        Email = user.Email,
                        Name = user.Name,
                        Roles = user.Roles
                    },
                Message = error
            };
        }
    }
}
