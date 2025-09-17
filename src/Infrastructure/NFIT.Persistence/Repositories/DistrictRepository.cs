using NFIT.Application.Abstracts.Repositories;
using NFIT.Domain.Entities;
using NFIT.Persistence.Contexts;

namespace NFIT.Persistence.Repositories;

public class DistrictRepository:Repository<District>,IDistrictRepository
{
    public DistrictRepository(NFITDbContext context):base(context)
    {
        
    }
}
