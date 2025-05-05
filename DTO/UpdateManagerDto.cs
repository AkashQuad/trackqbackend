namespace server.DTO
{
    public class UpdateManagerDto
    {
        public int EmployeeId { get; set; }
        public int? ManagerId { get; set; } // Nullable in case an employee should have no manager

        public bool status { get; set; } = true;
    }
}
