using NFIT.Application.Abstracts.Repositories;
using NFIT.Domain.Entities;
using NFIT.Persistence.Contexts;

namespace NFIT.Persistence.Repositories;

public class ReviewRepository:Repository<Review>,IReviewRepository
{
    public ReviewRepository(NFITDbContext context):base(context)
    {
        
    }
}
