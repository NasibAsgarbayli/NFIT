using System.Reflection;

namespace NFIT.Application.Shared.Helpers;

public static class PermissionHelper
{

    public static Dictionary<string, List<string>> GetAllPermissions()
    {
        var result = new Dictionary<string, List<string>>();

        var nestedTypes = typeof(Permissions).GetNestedTypes(BindingFlags.Public |
            BindingFlags.Static);

        foreach (var moduleType in nestedTypes)
        {
            var allField = moduleType.GetField("All", BindingFlags.Public |
                BindingFlags.Static);

            if (allField != null)
            {
                var permissions = allField.GetValue(null) as List<string>;
                if (permissions != null)
                {
                    result.Add(moduleType.Name, permissions);
                }

            }
        }
        return result;
    }

    public static List<string> GetAllPermissionList()
    {
        return GetAllPermissions()
            .SelectMany(kv => kv.Value)
            .Distinct()
            .ToList();
    }
}
