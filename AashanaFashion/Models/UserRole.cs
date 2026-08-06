using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AashanaFashion.Models
{
    [Table("UserRoleList")]
    public class UserRole
    {
        public int Id { get; set; }

        [Required]
        public string RoleName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public List<RolePermission> Permissions { get; set; } = new();
    }
}
