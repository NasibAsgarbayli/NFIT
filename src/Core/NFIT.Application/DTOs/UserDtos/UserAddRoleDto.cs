namespace NFIT.Application.DTOs.UserDtos;

public class UserAddRoleDto
{
    public Guid UserId { get; set; }
    public List<Guid> RoleId { get; set; }
}
