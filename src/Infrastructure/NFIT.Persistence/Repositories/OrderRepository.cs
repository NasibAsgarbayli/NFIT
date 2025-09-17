using NFIT.Application.Abstracts.Repositories;
using NFIT.Domain.Entities;
using NFIT.Persistence.Contexts;

namespace NFIT.Persistence.Repositories;

public class OrderRepository:Repository<Order>,IOrderRepository
{
    public OrderRepository(NFITDbContext context):base(context)
    {
        
    }
}
