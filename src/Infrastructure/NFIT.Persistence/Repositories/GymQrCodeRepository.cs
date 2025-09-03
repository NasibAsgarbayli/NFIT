using NFIT.Application.Abstracts.Repositories;
using NFIT.Domain.Entities;
using NFIT.Persistence.Contexts;

namespace NFIT.Persistence.Repositories;

public class GymQrCodeRepository:Repository<GymQRCode>,IGymQrCodeRepository
{
    public GymQrCodeRepository(NFITDbContext context):base(context)
    {
        
    }
}
