import AuthManager from '@/api/AuthManager';
import {
  LoginResponseDto,
  SignUpResponseDto,
  OperationResultDto,
  RefreshTokenRequestDto,
  RefreshTokenResponseDto
} from '@/api/AppDtos';

const AuthClient = {
  async login(email: string, password: string): Promise<LoginResponseDto> {
    return AuthManager.Login({ Email: email, Password: password });
  },
  async signUp(email: string, password: string, name?: string): Promise<SignUpResponseDto> {
    return AuthManager.SignUp({ Email: email, Password: password, Name: name ?? null });
  },
  async signOut(): Promise<void> {
    localStorage.removeItem('auth_token');
    localStorage.removeItem('refresh_token');
    localStorage.removeItem('auth_user');
  },
  async changePassword(newPassword: string): Promise<OperationResultDto> {
    return AuthManager.ChangePassword({ NewPassword: newPassword });
  },
  async sendPasswordResetEmail(email: string, redirectTo: string): Promise<OperationResultDto> {
    return AuthManager.SendPasswordResetEmail({ Email: email, RedirectUrl: redirectTo });
  },
  async refreshToken(accessToken: string, refreshToken: string): Promise<RefreshTokenResponseDto> {
    return AuthManager.RefreshToken({AccessToken: accessToken, RefreshToken: refreshToken });
  }
};

export default AuthClient;
