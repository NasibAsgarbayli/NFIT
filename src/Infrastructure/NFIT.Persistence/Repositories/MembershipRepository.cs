using NFIT.Application.Abstracts.Repositories;
using NFIT.Domain.Entities;
using NFIT.Persistence.Contexts;

namespace NFIT.Persistence.Repositories;

public class MembershipRepository:Repository<Membership>,IMembershipRepository
{
    public MembershipRepository(NFITDbContext context):base(context)
    {
        
    }
}
