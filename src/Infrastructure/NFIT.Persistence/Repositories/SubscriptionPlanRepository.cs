using NFIT.Application.Abstracts.Repositories;
using NFIT.Domain.Entities;
using NFIT.Persistence.Contexts;

namespace NFIT.Persistence.Repositories;

public class SubscriptionPlanRepository:Repository<SubscriptionPlan>,ISubscriptionPlanRepository
{
    public SubscriptionPlanRepository(NFITDbContext context):base(context)
    {
        
    }
}
