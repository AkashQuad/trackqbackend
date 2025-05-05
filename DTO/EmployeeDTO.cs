namespace server.DTO
{
    public class EmployeeDTO
    {
        public int EmployeeId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public DateTime JoinedDate { get; set; }
        public int RoleID { get; set; }
        public string? Role { get; set; }
        public int? ManagerId { get; set; }

        public bool status { get; set; } = true;
    }
    public class CreateEmployeeDto
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public int RoleId { get; set; }
        public int? ManagerId { get; set; }
        public bool status { get; set; } = true;
    }
}