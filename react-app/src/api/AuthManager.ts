import { LoginRequestDto, LoginResponseDto, SignUpRequestDto, SignUpResponseDto, SendPasswordResetRequestDto, OperationResultDto, ChangePasswordRequestDto, RefreshTokenRequestDto, RefreshTokenResponseDto, UpdateUserEmailDto, UpdateUserEmailResponseDto, UpdateUserNameDto, UpdateUserNameResponseDto, UpdateUserPasswordDto, UpdateUserPasswordResponseDto } from "./AppDtos";
import ApiClient from "./ApiClient";

const Login = (request: LoginRequestDto): Promise<LoginResponseDto> =>
  ApiClient.invokeMethod<LoginResponseDto>("Api", "AuthManager", "Login", request);

const SignUp = (request: SignUpRequestDto): Promise<SignUpResponseDto> =>
  ApiClient.invokeMethod<SignUpResponseDto>("Api", "AuthManager", "SignUp", request);

const SendPasswordResetEmail = (request: SendPasswordResetRequestDto): Promise<OperationResultDto> =>
  ApiClient.invokeMethod<OperationResultDto>("Api", "AuthManager", "SendPasswordResetEmail", request);

const ChangePassword = (request: ChangePasswordRequestDto): Promise<OperationResultDto> =>
  ApiClient.invokeMethod<OperationResultDto>("Api", "AuthManager", "ChangePassword", request);

const UpdateUserEmail = (request: UpdateUserEmailDto): Promise<UpdateUserEmailResponseDto> =>
  ApiClient.invokeMethod<UpdateUserEmailResponseDto>("Api", "AuthManager", "UpdateUserEmail", request);

const UpdateUserPassword = (request: UpdateUserPasswordDto): Promise<UpdateUserPasswordResponseDto> =>
  ApiClient.invokeMethod<UpdateUserPasswordResponseDto>("Api", "AuthManager", "UpdateUserPassword", request);

const UpdateUserName = (request: UpdateUserNameDto): Promise<UpdateUserNameResponseDto> =>
  ApiClient.invokeMethod<UpdateUserNameResponseDto>("Api", "AuthManager", "UpdateUserName", request);

const RefreshToken = (request: RefreshTokenRequestDto): Promise<RefreshTokenResponseDto> =>
  ApiClient.invokeMethod<RefreshTokenResponseDto>("Api", "AuthManager", "RefreshToken", request);



export default {
  Login,
  SignUp,
  SendPasswordResetEmail,
  ChangePassword,
  UpdateUserEmail,
  UpdateUserPassword,
  UpdateUserName,
  RefreshToken
};
