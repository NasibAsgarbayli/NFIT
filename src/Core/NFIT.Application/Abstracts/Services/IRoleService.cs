using NFIT.Application.DTOs.RoleDtos;
using NFIT.Application.Shared;

namespace NFIT.Application.Abstracts.Services;

public interface IRoleService
{

    Task<BaseResponse<string?>> CreateRole(RoleCreateDto dto);
    Task<BaseResponse<string?>> UpdateRole(RoleUpdateDto dto);
    Task<BaseResponse<List<RoleWithPermissionsDto>>> GetAllRolesWithPermissionsAsync();

    Task<BaseResponse<string?>> DeleteRoleAsync(string roleName);


}
