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

}

