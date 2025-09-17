using Microsoft.Extensions.DependencyInjection;
using NFIT.Application.Abstracts.Repositories;
using NFIT.Application.Abstracts.Services;
using NFIT.Infrastructure.Services;
using NFIT.Persistence.Repositories;
using NFIT.Persistence.Services;

namespace NFIT.Persistence;

public static class ServiceRegistration
{
    public static void RegisterService(this IServiceCollection services)
    {
        #region Repositoires
        services.AddScoped<IGymRepository, GymRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IDistrictRepository, DistrictRepository>();
        services.AddScoped<IExerciseRepository, ExerciseRepository>();
        services.AddScoped<IFavouriteRepository, FavouriteRepository>();
        services.AddScoped<IGymCheckInRepository, GymCheckInRepository>();
        services.AddScoped<IGymQrCodeRepository, GymQrCodeRepository>();
        services.AddScoped<IImageRepository, ImageRepository>();
        services.AddScoped<IMembershipRepository, MembershipRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
        services.AddScoped<ISupplementRepository, SupplementRepository>();
        services.AddScoped<ITrainerRepository, TrainerRepository>();
        services.AddScoped<ITrainerWorkoutRepository, TrainerWorkoutRepository>();
        services.AddScoped<IWorkoutRepository, WorkoutRepository>();
        #endregion


        #region Servcices
        services.AddScoped<IFileService, FileService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IAuthentication, Authentication>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IGymService, GymService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ISubscriptionPlanService, SubscriptionPlanService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IFavouriteService, FavouriteService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IDistrictService, DistrictService>();
       
        #endregion
    }
}
