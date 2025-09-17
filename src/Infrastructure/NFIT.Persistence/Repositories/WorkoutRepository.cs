using NFIT.Application.Abstracts.Repositories;
using NFIT.Domain.Entities;
using NFIT.Persistence.Contexts;

namespace NFIT.Persistence.Repositories;

public class WorkoutRepository:Repository<Workout>,IWorkoutRepository
{
    public WorkoutRepository(NFITDbContext context):base(context)   
    {
        
    }
}
