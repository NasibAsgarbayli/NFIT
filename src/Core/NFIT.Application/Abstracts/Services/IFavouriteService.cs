using NFIT.Application.DTOs.FavouriteDtos;
using NFIT.Application.Shared;

namespace NFIT.Application.Abstracts.Services;

public interface IFavouriteService
{
    Task<BaseResponse<string>> CreateAsync(FavouriteAddDto dto);
    Task<BaseResponse<string>> DeleteByIdAsync(Guid favouriteId);

    Task<BaseResponse<FavouriteListResponseDto>> GetAllMyFavouriteAsync();


    Task<BaseResponse<List<FavouriteListItemDto>>> GetUserFavouriteGymsAsync();
    Task<BaseResponse<List<FavouriteListItemDto>>> GetUserFavouriteTrainersAsync();
    Task<BaseResponse<List<FavouriteListItemDto>>> GetUserFavouriteSupplementsAsync();

    Task<BaseResponse<string>> ClearUserFavouritesAsync();
}
