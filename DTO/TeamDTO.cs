using System.ComponentModel.DataAnnotations;

namespace server.DTO
{
    public class TeamDTO
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public int ManagerId { get; set; }
        public string ManagerName { get; set; }
        public List<TeamMemberDTO> Members { get; set; } = new List<TeamMemberDTO>();
    }
    public class TeamMemberDTO
    {
        public int TeamMemberId { get; set; }
        public int EmployeeId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public DateTime JoinedDate { get; set; }
    }

    public class CreateTeamDTO
    {
        [Required]
        public string TeamName { get; set; }

        public string Description { get; set; }

        [Required]
        public int ManagerId { get; set; }

        [Required]
        public List<int> MemberIds { get; set; } = new List<int>();
    }

    public class AddTeamMembersDTO
    {
        [Required]
        public int TeamId { get; set; }

        [Required]
        public List<int> EmployeeIds { get; set; } = new List<int>();
    }

    public class RemoveTeamMemberDTO
    {
        [Required]
        public int TeamId { get; set; }

        [Required]
        public int EmployeeId { get; set; }
    }
}
