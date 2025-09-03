using NFIT.Application.Abstracts.Repositories;
using NFIT.Domain.Entities;
using NFIT.Persistence.Contexts;

namespace NFIT.Persistence.Repositories;

public class CategoryRepository:Repository<Category>,ICategoryRepository
{
    public CategoryRepository(NFITDbContext context):base(context)
    {
        
    }
}
