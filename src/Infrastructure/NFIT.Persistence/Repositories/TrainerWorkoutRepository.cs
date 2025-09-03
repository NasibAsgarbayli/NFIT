using NFIT.Application.Abstracts.Repositories;
using NFIT.Domain.Entities;
using NFIT.Persistence.Contexts;

namespace NFIT.Persistence.Repositories;

public class TrainerWorkoutRepository:Repository<TrainerWorkout>, ITrainerWorkoutRepository
{
    public TrainerWorkoutRepository(NFITDbContext context):base(context)
    {
        
    }
}
