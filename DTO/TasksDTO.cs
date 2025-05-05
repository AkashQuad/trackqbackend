using System.ComponentModel.DataAnnotations;
using server.Model;

namespace server.DTO
{
    public class TasksDTO
    {
        [Key]

        public int TaskId { get; set; }
        public int EmployeeId { get; set; }
        public string Topic { get; set; }
        public string SubTopic { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }
        public int ExpectedHours { get; set; }
        public int CompletedHours { get; set; }
        public int priority { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = "Pending";

        public int? AssignedBy { get; set; }

        public string? AssignedManager { get; set; }
        public DateTime? AssignedDate { get; set; }
    }
    public class TaskDateDTO
    {
        public string Date { get; set; }
    }
    public class DailyTaskHoursDto
    {
        public DateTime Date { get; set; }
        public int HoursSpent { get; set; }
        
    }
}