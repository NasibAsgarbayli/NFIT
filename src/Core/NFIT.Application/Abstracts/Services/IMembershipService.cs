using NFIT.Application.DTOs.MembershipDtos;
using NFIT.Application.Shared;

namespace NFIT.Application.Abstracts.Services;

public interface IMembershipService
{
    Task<BaseResponse<MembershipGetDto>> GetMyMembershipAsync();     // aktiv (yoxdursa ən son)
    Task<BaseResponse<string>> CancelMyMembershipAsync();            // IsActive=false, EndDate=now
    Task<BaseResponse<string>> DeleteAsync(Guid id);
    Task<BaseResponse<List<MembershipListItemDto>>> GetMyMembershipHistoryAsync();// yalnız özünə aid olanı silə bilər (soft)

    // Admin: istənilən istifadəçinin membership-i (aktiv yoxdursa ən son)
    Task<BaseResponse<MembershipGetDto>> GetUsersMembershipAsync(string userId);
    Task<BaseResponse<string>> DeactivateUserMembershipAsync(string userId);
    Task<bool> HasActiveMembershipForGymAsync(string userId, Guid gymId);
    Task<BaseResponse<Guid>> CreateFromDeliveredOrderAsync(MembershipCreateFromOrderDto dto);
}
