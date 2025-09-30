namespace NFIT.Application.DTOs.UserDtos;

public class UserGetDto
{
    public string Id { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public List<string> Roles { get; set; } = new();

}
