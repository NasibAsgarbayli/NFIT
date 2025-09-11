namespace NFIT.Application.DTOs.RoleDtos;

public class RoleUpdateDto
{

    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public List<string> PermissionList { get; set; } = new();
}
