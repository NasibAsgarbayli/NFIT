using NFIT.Application.DTOs.ReviewDtos;
using NFIT.Application.Shared;

namespace NFIT.Application.Abstracts.Services;

public interface IReviewService
{
    Task<BaseResponse<Guid>> CreateAsync(ReviewCreateDto dto);          // current user
    Task<BaseResponse<string>> UpdateAsync(ReviewUpdateDto dto);        // only owner
    Task<BaseResponse<string>> DeleteByIdAsync(Guid reviewId);          // owner or admin

    Task<BaseResponse<string>> ApproveReviewAsync(Guid reviewId);       // admin/mod
    Task<BaseResponse<List<ReviewGetDto>>> GetApprovedReviewsAsync(ReviewQueryDto query);
    Task<BaseResponse<decimal>> GetAverageRatingAsync(ReviewQueryDto query); // only approved

    Task<BaseResponse<bool>> HasUserReviewedAsync(ReviewCreateDto dto); // current user + target
    Task<BaseResponse<List<ReviewGetDto>>> GetMyReviewsAsync();         // current user
    Task<BaseResponse<string>> ClearAllMyReviewsAsync();
}
