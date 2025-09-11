namespace NFIT.Application.DTOs.AuthenticationDtos;

public class ResetPasswordDto
{
    public string Email { get; set; } = null!;

    public string Token { get; set; } = null!;

    public string NewPassword { get; set; } = null!;
}
