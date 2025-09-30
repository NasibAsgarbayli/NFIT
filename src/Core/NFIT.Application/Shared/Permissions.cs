namespace NFIT.Application.Shared;

public class Permissions
{

    public static class Account
    {
        public const string AddRole = "Account.AddRole";
        public const string Create = "Account.Create";

        public static List<string> All = new()
        {
            AddRole,
            Create

        };
    }



    public static class User
    {

        public const string ResetPaswword = "User.ResetPassword";
        public const string GetAll = "User.GetAll";
        public const string GetById = "User.GetById";
        public const string Create = "User.Create";

        public static List<string> All = new()
        {
            GetAll,
            GetById,
            ResetPaswword,
            Create
        };
    }

    public static class Role
    {
        public const string Create = "Role.Create";
        public const string Delete = "Role.Delete";
        public const string Update = "Role.Update";
        public const string GetAllPermissions = "Role.GetAllPermissions";

        public static List<string> All = new()
        {
            Create,
            Delete,
            Update,
            GetAllPermissions
        };
    }
    public static class Order
    {
        public const string ViewSales = "Order.ViewSales";
        public const string Delete = "Orders.Delete";
        public const string UpdateStatus = "Orders.UpdateStatus";
        public const string Confirm = "Orders.Confirm";
        public const string GetSales = "Orders.GetSales";

        public static List<string> All = new()
        {
            ViewSales, 
            Delete,
            GetSales,
            UpdateStatus,
            Confirm
        };
    }


    public static class Category
    {
        public const string Create = "Category.Create";
        public const string Update = "Category.Update";
        public const string Delete = "Category.Delete";
        

        public static List<string> All = new()
        {
            Create,
            Update, 
            Delete, 
           
        };
    }
    public static class District
    {
        public const string Create = "District.Create";
        public const string Update = "District.Update";
        public const string Deactivate = "District.Deactivate";
        public const string Delete = "District.Delete";
        public const string Activate = "District.Activate";

        // Admin və Moderator rollarına assign etmək üçün toplu siyahı
        public static readonly List<string> All = new()
        {
            Create,
            Update,
            Deactivate,
            Delete,
            Activate
           
        };
    }

    public static class Exercise
    {
        public const string Create = "Exercises.Create";
        public const string Update = "Exercises.Update";
        public const string Delete = "Exercises.Delete";

        public static readonly List<string> All = new()
        {
            Create,
            Update,
            Delete

        };
    }
    public static class Favourite
    {
        public const string Create = "Favourite.Create";
        public const string Delete = "Favourite.Delete";

        public static List<string> All = new()
        {
            Create,
            Delete
        };
    }
    public static class Gym
    {
        public const string Create = "Gym.Create";
        public const string Update = "Gym.Update";
        public const string Delete = "Gym.Delete";
        public const string AddImage = "Gym.AddImage";
        public const string DeleteImage = "Gym.DeleteImage";

        public static List<string> All = new()
            {
                Create,
                Update,
                Delete,
                AddImage,
                DeleteImage
            };
    }
    public static class Membership
    {
        // ---- Admin və Moderator üçün ----
        public const string DeactivateUser = "Membership.DeactivateUser"; // İstənilən istifadəçinin abonementini deaktiv et
        public const string ViewUser = "Membership.ViewUser";       // İstənilən istifadəçinin abonement tarixçəsini gör
        public const string Delete = "Membership.Delete";         // İstənilən abonementi sil (soft delete)

        
        public static List<string> All = new()
            {
                DeactivateUser,
                ViewUser,
                Delete
            };
    }


    public static class Review
    {
        public const string Create = "Review.Create";
        public const string Update = "Review.Update";
        public const string Approve = "Review.Approve";
        public const string Delete = "Review.Delete";
        public const string HasReviewed = "Review.HasReviewed";
        

        // Admin və Moderator rollarına assign etmək üçün toplu siyahı
        public static readonly List<string> All = new()
        {
            Create,
            Update,
            Approve,
            Delete,
            HasReviewed

        };
    }


    public static class Subscription
    {
        public const string Create = "Subscription.Create";
        public const string Update = "Subscription.Update";
        public const string Delete = "Subscription.Delete";

        public static readonly List<string> All = new()
        {
            Create,
            Update,
            Delete

        };
    }



    public static class Supplement
    {
        public const string Create = "Supplement.Create";
        public const string Update = "Supplement.Update";
        public const string Delete = "Supplement.Delete";
        public const string AddImage = "Supplement.AddImage";
        public const string DeleteImage = "Supplement.DeleteImage";

        public static readonly List<string> All = new()
        {
            Create,
            Update,
            Delete,
            AddImage,
            DeleteImage

        };
    }

    public static class Workout
    {
        public const string Create = "Workout.Create";
        public const string Update = "Workout.Update";
        public const string Delete = "Workout.Delete";

        public static readonly List<string> All = new()
        {
            Create,
            Update,
            Delete

        };
    }


    public static class Trainer
    {
        public const string Create = "Trainer.Create";
        public const string Update = "Trainer.Update";
        public const string Delete = "Trainer.Delete";
        public const string Verify = "Trainer.Verify";
        public const string ToggleActive = "Trainer.ToggleActive";
        public const string AddImageAsync = "Trainer.AddImageAsync";
        public const string DeleteImageAsync = "Trainer.DeleteImageAsync";
        public const string CreateVideo = "Trainer.CreateVideo";
        public const string UpdateVideo = "Trainer.UpdateVideo";
        public const string UploadVideo = "Trainer.UploadVideo";
        public const string DeleteVideo = "Trainer.DeleteVideo";
        public const string UploadVideoThumb = "Trainer.UploadVideoThumb";
        public const string CreateWorkout = "Trainer.CreateWorkout";
        public const string UpdateWorkout = "Trainer.UpdateWorkout";
        public const string DeleteWorkout = "Trainer.DeleteWorkout";
        public const string UploadWorkoutThumb = "Trainer.UploadWorkoutThumb";
        public const string UploadWorkoutPreview = "Trainer.UploadWorkoutPreview";

        public static readonly List<string> All = new()
        {
            Create,
            Update,
            Delete,
            Verify,
            ToggleActive,
            AddImageAsync,
            DeleteImageAsync,
            UploadVideo,
            UploadVideoThumb,
            UploadWorkoutPreview,
            CreateVideo,
            UpdateVideo,
            UploadVideoThumb,
            UploadWorkoutPreview,
            CreateWorkout,
            UpdateWorkout,
            DeleteWorkout,
            DeleteVideo


        };
    }
}


