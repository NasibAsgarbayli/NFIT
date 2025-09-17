using NFIT.Application.DTOs.UserDtos;
using NFIT.Application.Shared;

namespace NFIT.Application.Abstracts.Services;

public interface IUserService
{

    Task<BaseResponse<string>> AddRole(UserAddRoleDto dto);
    Task<BaseResponse<List<UserGetDto>>> GetAllAsync();
    Task<BaseResponse<UserGetDto>> GetByIdAsync(Guid id);


}
