using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NFIT.Application.Abstracts.Services;
using NFIT.Application.DTOs.RoleDtos;
using NFIT.Application.Shared;
using NFIT.Application.Shared.Helpers;

namespace NFIT.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly IRoleService _roleService;

        public RolesController(IRoleService roleService)
        {
            _roleService = roleService;
        }


        // GET: api/<RolesController>
     
        [HttpPost("CreateRole")]
        [Authorize(Policy = Permissions.Role.Create)]
        public async Task<IActionResult> Create(RoleCreateDto dto)
        {
            var result = await _roleService.CreateRole(dto);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = Permissions.Role.Update)]
        public async Task<IActionResult> Update(string id, [FromBody] RoleUpdateDto dto)
        {
            if (id != dto.Id)
                return BadRequest("ID mismatch");

            var result = await _roleService.UpdateRole(dto);
            return StatusCode((int)result.StatusCode, result);
        }


        [HttpGet("Roles and Their Permissions")]
        [Authorize(Policy = Permissions.Role.GetAllPermissions)]
        public async Task<IActionResult> GetAllRoles()
        {
            var result = await _roleService.GetAllRolesWithPermissionsAsync();
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("permissions")]
        [Authorize(Policy = Permissions.Role.GetAllPermissions)]
        public IActionResult GetAllPermissions()
        {
            var permissions = PermissionHelper.GetAllPermissions();
            return Ok(permissions);
        }


        [HttpDelete("{roleName}")]
        [Authorize(Policy = Permissions.Role.Delete)]
        public async Task<IActionResult> DeleteRole(string roleName)
        {
            var result = await _roleService.DeleteRoleAsync(roleName);
            return StatusCode((int)result.StatusCode, result);
        }

    }
}
