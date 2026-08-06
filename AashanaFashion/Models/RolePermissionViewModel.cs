using System.ComponentModel.DataAnnotations;

namespace AashanaFashion.Models
{
    public class RolePermissionViewModel
    {
        public int UserRoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public List<ModulePermissionItem> Modules { get; set; } = new();
    }

    public class ModulePermissionItem
    {
        public string Module { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool CanView { get; set; }
        public bool CanCreate { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }
}
