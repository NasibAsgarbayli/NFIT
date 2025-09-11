namespace NFIT.Application.DTOs.RoleDtos;

public class RoleWithPermissionsDto
{
    public string Name { get; set; } = null!;
    public List<string> Permissions { get; set; } = new();
    public string Id { get; set; }
}
