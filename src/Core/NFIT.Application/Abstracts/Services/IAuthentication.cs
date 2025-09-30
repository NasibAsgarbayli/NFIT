using System.Security.Claims;
using NFIT.Application.DTOs.AuthenticationDtos;
using NFIT.Application.Shared;

namespace NFIT.Application.Abstracts.Services;

public interface IAuthentication
{
    Task<BaseResponse<string>> Register(UserRegisterDto dto);
    Task<BaseResponse<TokenResponse>> Login(UserLoginDto dto);
    Task<BaseResponse<string>> LogoutAsync(ClaimsPrincipal userPrincipal);
    Task<BaseResponse<ProfileInfoDto>> GetProfileAsync(ClaimsPrincipal userPrincipal);

    Task<BaseResponse<string>> ConfirmEmail(string userId, string token);

    Task<BaseResponse<TokenResponse>> RefreshTokenAsync(RefreshTokenRequest request);

    Task<BaseResponse<string>> SendResetPasswordEmail(string email);

    Task<BaseResponse<string>> ResetPasswordAsync(ResetPasswordDto dto);
}
