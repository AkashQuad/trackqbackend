using System.ComponentModel.DataAnnotations;

namespace server.Model
{
    public class MailRequest
    {
        [EmailAddress]
        public string Email { get; set; }
        public string Subject { get; set; }
        public string Emailbody { get; set; }
    }
}
