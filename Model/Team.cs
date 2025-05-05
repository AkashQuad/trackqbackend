using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace server.Model
{
    public class Team
    {
        [Key]
        public int TeamId { get; set; }

        [Required]
        [StringLength(100)]
        public string TeamName { get; set; }

        public string Description { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Foreign Key to Manager
        public int ManagerId { get; set; }

        [ForeignKey("ManagerId")]
        [JsonIgnore]
        public virtual Employee Manager { get; set; }

        // Navigation property for team members
        public virtual ICollection<TeamMember> TeamMembers { get; set; } = new List<TeamMember>();
    }
    public class TeamMember
    {
        [Key]
        public int TeamMemberId { get; set; }

        // Foreign Key to Team
        public int TeamId { get; set; }

        [ForeignKey("TeamId")]
        [JsonIgnore]
        public virtual Team Team { get; set; }

        // Foreign Key to Employee
        public int EmployeeId { get; set; }

        [ForeignKey("EmployeeId")]
        [JsonIgnore]
        public virtual Employee Employee { get; set; }

        public DateTime JoinedDate { get; set; } = DateTime.UtcNow;
    }
}
