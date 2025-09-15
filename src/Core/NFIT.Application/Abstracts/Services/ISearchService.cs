using NFIT.Application.DTOs.GymDtos;
using NFIT.Application.DTOs.SearchDtos;
using NFIT.Application.Shared;

namespace NFIT.Application.Abstracts.Services;

public interface ISearchService
{
    Task<BaseResponse<List<GymListItemDto>>> SearchGymsAsync(SearchGymsRequest request);
}
