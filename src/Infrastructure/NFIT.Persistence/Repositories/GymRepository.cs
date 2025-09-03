using NFIT.Application.Abstracts.Repositories;
using NFIT.Domain.Entities;
using NFIT.Persistence.Contexts;

namespace NFIT.Persistence.Repositories;

public class GymRepository:Repository<Gym>,IGymRepository
{
	public GymRepository(NFITDbContext context):base(context)
	{

	}
}
