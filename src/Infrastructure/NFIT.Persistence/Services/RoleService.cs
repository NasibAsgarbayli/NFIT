using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using NFIT.Application.Abstracts.Services;
using NFIT.Application.DTOs.RoleDtos;
using NFIT.Application.Shared;

namespace NFIT.Persistence.Services;

public class RoleService:IRoleService
{
    private readonly RoleManager<IdentityRole> _rolemanager;

    public RoleService(RoleManager<IdentityRole> rolemanager)
    {
        _rolemanager = rolemanager;
    }

    public async Task<BaseResponse<string?>> CreateRole(RoleCreateDto dto)
    {

        var existingRole = await _rolemanager.FindByNameAsync(dto.Name);
        if (existingRole is not null)
        {
            return new BaseResponse<string?>("Role Already Exists", HttpStatusCode.BadRequest);
        }
        var identityRole = new IdentityRole(dto.Name);
        var result = await _rolemanager.CreateAsync(identityRole);
        if (!result.Succeeded)
        {
            var errorMessages = string.Join(", ", result.Errors.Select(e => e.Description));
            return new BaseResponse<string?>(errorMessages, HttpStatusCode.BadRequest);
        }

        foreach (var permission in dto.PermissionList.Distinct())
        {
            var claimResult = await _rolemanager.AddClaimAsync(identityRole, new Claim("Permission", permission));
            if (!claimResult.Succeeded)
            {
                var error = string.Join(", ", claimResult.Errors.Select(e => e.Description));
                return new BaseResponse<string?>($"Role created,but adding permission '{permission}' failed:{error}", HttpStatusCode.PartialContent);
            }
        }
        return new BaseResponse<string?>("Role created succesfuly", true, HttpStatusCode.Created);
    }

    public async Task<BaseResponse<string?>> UpdateRole(RoleUpdateDto dto)
    {
        var existingRole = await _rolemanager.FindByIdAsync(dto.Id);
        if (existingRole is null)
        {
            return new BaseResponse<string?>("Role not found", HttpStatusCode.NotFound);
        }

        existingRole.Name = dto.Name;
        var updateResult = await _rolemanager.UpdateAsync(existingRole);
        if (!updateResult.Succeeded)
        {
            var errorMessages = string.Join(", ", updateResult.Errors.Select(e => e.Description));
            return new BaseResponse<string?>(errorMessages, HttpStatusCode.BadRequest);
        }

        // Mövcud permission-ləri sil
        var currentClaims = await _rolemanager.GetClaimsAsync(existingRole);
        var permissionClaims = currentClaims.Where(c => c.Type == "Permission").ToList();

        foreach (var claim in permissionClaims)
        {
            await _rolemanager.RemoveClaimAsync(existingRole, claim);
        }

        // Yeni permission-ləri əlavə et
        foreach (var permission in dto.PermissionList.Distinct())
        {
            var claimResult = await _rolemanager.AddClaimAsync(existingRole, new Claim("Permission", permission));
            if (!claimResult.Succeeded)
            {
                var error = string.Join(", ", claimResult.Errors.Select(e => e.Description));
                return new BaseResponse<string?>($"Role updated, but adding permission '{permission}' failed: {error}", HttpStatusCode.PartialContent);
            }
        }

        return new BaseResponse<string?>("Role updated successfully", true, HttpStatusCode.OK);
    }



    public async Task<BaseResponse<List<RoleWithPermissionsDto>>> GetAllRolesWithPermissionsAsync()
    {
        var roleList = new List<RoleWithPermissionsDto>();
        var roles = _rolemanager.Roles.ToList();

        foreach (var role in roles)
        {
            var claims = await _rolemanager.GetClaimsAsync(role);
            var permissions = claims
                .Where(c => c.Type == "Permission")
                .Select(c => c.Value)
                .Distinct()
                .ToList();

            roleList.Add(new RoleWithPermissionsDto
            {
                Id = role.Id,
                Name = role.Name!,
                Permissions = permissions
            });
        }

        return new BaseResponse<List<RoleWithPermissionsDto>>("Role list with permissions returned successfully", roleList, HttpStatusCode.OK);
    }





    public async Task<BaseResponse<string?>> DeleteRoleAsync(string roleName)
    {
        var role = await _rolemanager.FindByNameAsync(roleName);
        if (role == null)
            return new BaseResponse<string?>("Role not found", HttpStatusCode.NotFound);

        var result = await _rolemanager.DeleteAsync(role);
        if (!result.Succeeded)
        {
            var errorMessages = string.Join(", ", result.Errors.Select(e => e.Description));
            return new BaseResponse<string?>(errorMessages, HttpStatusCode.BadRequest);
        }

        return new BaseResponse<string?>("Role deleted successfully", true, HttpStatusCode.OK);
    }

}
