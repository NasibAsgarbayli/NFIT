using NFIT.Application.Abstracts.Repositories;
using NFIT.Domain.Entities;
using NFIT.Persistence.Contexts;

namespace NFIT.Persistence.Repositories;

public class ImageRepository:Repository<Image>,IImageRepository
{
    public ImageRepository(NFITDbContext context):base(context)
    {
        
    }
}
