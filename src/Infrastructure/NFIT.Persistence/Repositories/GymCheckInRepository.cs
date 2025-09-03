using NFIT.Application.Abstracts.Repositories;
using NFIT.Domain.Entities;
using NFIT.Persistence.Contexts;

namespace NFIT.Persistence.Repositories;

public class GymCheckInRepository:Repository<GymCheckIn>,IGymCheckInRepository
{
    public GymCheckInRepository(NFITDbContext context):base(context)
    {
        
    }
}
