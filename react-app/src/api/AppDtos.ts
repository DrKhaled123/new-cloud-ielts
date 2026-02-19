
export interface ChangePasswordRequestDto {
  NewPassword: string;
}

export interface CreateUserRequest {
  Name: string;
  Email: string;
}

export interface LoginRequestDto {
  Email: string;
  Password: string;
}

export interface LoginResponseDto {
  Success: boolean;
  Message: string | null;
  JwtToken: string | null;
  RefreshToken: string | null;
  User: UserDto | null;
}

export interface OperationResultDto {
  Success: boolean;
  Message: string;
}

export interface RefreshTokenRequestDto {
  AccessToken: string;
  RefreshToken: string;
}

export interface RefreshTokenResponseDto {
  Success: boolean;
  JwtToken: string | null;
  RefreshToken: string | null;
  User: UserDto | null;
  Message: string | null;
}


export interface SendPasswordResetRequestDto {
  Email: string;
  RedirectUrl: string | null;
}

export interface ServiceInvocationRequestDto {
  ManagerName: string;
  MethodName: string;
  Parameters: any | null[] | null;
}

export interface SignUpRequestDto {
  Email: string;
  Password: string;
  Name: string | null;
}

export interface SignUpResponseDto {
  Success: boolean;
  Message: string | null;
  User: UserDto | null;
}

export interface UpdateUserEmailDto {
  NewEmail: string;
}

export interface UpdateUserEmailResponseDto {
  Success: boolean;
  Message: string;
}

export interface UpdateUserNameDto {
  NewName: string;
}

export interface UpdateUserNameResponseDto {
  Success: boolean;
  Message: string;
}

export interface UpdateUserPasswordDto {
  NewPassword: string;
}

export interface UpdateUserPasswordResponseDto {
  Success: boolean;
  Message: string;
}

export interface UserDto {
  Email: string;
  Name: string;
  AuthId: string;
  Roles: string[];
}

export interface ValidateSessionResponse {
  Valid: boolean;
  Token: string;
  UserId: string;
  Message: string;
}

