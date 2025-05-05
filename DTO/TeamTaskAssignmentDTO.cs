namespace server.DTO
{
    public class TeamTaskAssignmentDTO
    {
        public int TeamId { get; set; }
        public string Topic { get; set; }
        public string SubTopic { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public double ExpectedHours { get; set; }
        public int priority { get; set; }
        public string Status { get; set; }
        public int AssignedBy { get; set; }
    }
}
