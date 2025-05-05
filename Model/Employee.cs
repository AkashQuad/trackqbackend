using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace server.Model
{
    public class Employee
    {
        [Key]
        public int EmployeeId { get; set; }

        [Required]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public DateTime JoinedDate { get; set; } = DateTime.UtcNow;

        // Foreign Key
        public int RoleID { get; set; }

        [ForeignKey("RoleID")]
        [JsonIgnore]
        public virtual Role Role { get; set; }

        // New property for ManagerId
        public int? ManagerId { get; set; } // Nullable to allow for employees without a manager

        [ForeignKey("ManagerId")]
        [JsonIgnore]
        public virtual Employee Manager { get; set; }

        // Navigation property for employees managed by this employee
        public virtual ICollection<Employee> ManagedEmployees { get; set; } = new List<Employee>();

        public bool status { get; set; } = true;
    }
}