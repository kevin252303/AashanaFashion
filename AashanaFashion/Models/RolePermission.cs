using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AashanaFashion.Models
{
    [Table("RolePermissions")]
    public class RolePermission
    {
        public int Id { get; set; }

        public int UserRoleId { get; set; }
        public UserRole? UserRole { get; set; }

        [Required]
        public string Module { get; set; } = string.Empty; // e.g. ProductionOrder, DesignMaster, VendorMaster

        public bool CanView { get; set; }
        public bool CanCreate { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }
}
