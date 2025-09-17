using NFIT.Application.Abstracts.Repositories;
using NFIT.Domain.Entities;
using NFIT.Persistence.Contexts;

namespace NFIT.Persistence.Repositories;

public class FavouriteRepository:Repository<Favourite>,IFavouriteRepository
{
    public FavouriteRepository(NFITDbContext context):base(context)
    {
        
    }
}
