using NFIT.Application.Abstracts.Repositories;
using NFIT.Domain.Entities;
using NFIT.Persistence.Contexts;

namespace NFIT.Persistence.Repositories;

public class ExerciseRepository:Repository<Exercise>,IExerciseRepository
{
    public ExerciseRepository(NFITDbContext context):base(context)
    {
        
    }
}
