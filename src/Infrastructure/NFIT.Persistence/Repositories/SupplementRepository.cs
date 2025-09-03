using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NFIT.Application.Abstracts.Repositories;
using NFIT.Domain.Entities;
using NFIT.Persistence.Contexts;

namespace NFIT.Persistence.Repositories;

public class SupplementRepository:Repository<Supplement>,ISupplementRepository
{
    public SupplementRepository(NFITDbContext context):base(context)    
    {
        
    }
}
