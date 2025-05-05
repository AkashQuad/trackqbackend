namespace server.DTO
{
    public class RegisterDTO
    {

        public string Email { get; set; }

        public string username { get; set; }
        public string Password { get; set; }

        public string role { get; set; } = "User";

        // Assign role (e.g., User = 1) and default as user
    }

}
