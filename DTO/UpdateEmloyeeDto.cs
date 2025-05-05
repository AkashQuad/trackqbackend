namespace server.DTO
{
    public class UpdateEmployeeDto
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public int RoleId { get; set; }

        public bool status { get; set; } = true;
        public int? ManagerId { get; set; }
    }

}
