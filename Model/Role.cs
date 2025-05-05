using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace server.Model
{
    public class Role
    {
        [Key]
        public int RoleID { get; set; }

        [Required]
        [StringLength(50)]
        public string RoleName { get; set; } = string.Empty; // Default empty string

        public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>(); // Initialize to avoid null
    }
}
