using System.ComponentModel.DataAnnotations;

namespace server.Model
{
    public class Tasks
    {
        [Key]
        public int TaskId { get; set; } // Auto-generated
        public int EmployeeId { get; set; } // Foreign key reference to Employee
        public string Topic { get; set; }
        public string SubTopic { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }
        public int ExpectedHours { get; set; }
        public int CompletedHours { get; set; }
        public int priority {get; set;}
        public DateTime StartDate {get;set;}
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = "Not Started"; // Default value

        public int? AssignedBy { get; set; }
        public DateTime? AssignedDate { get; set; }

        public string? AssignedManager { get; set; }

        public List<DailyTaskHours> DailyHours { get; set; } = new List<DailyTaskHours>();
    }
    public class DailyTaskHours
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public DateTime Date { get; set; }
        public int HoursSpent { get; set; }
        public Tasks Task { get; set; }
    }
}
