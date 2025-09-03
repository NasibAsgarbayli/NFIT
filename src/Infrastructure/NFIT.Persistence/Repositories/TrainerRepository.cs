using NFIT.Application.Abstracts.Repositories;
using NFIT.Domain.Entities;
using NFIT.Persistence.Contexts;

namespace NFIT.Persistence.Repositories;

public class TrainerRepository:Repository<Trainer>,ITrainerRepository
{
    public TrainerRepository(NFITDbContext context):base(context)
    {
        
    }
}
