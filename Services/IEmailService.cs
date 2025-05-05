using server.Model;

namespace server.services

{

    public interface IEmailService

    {

        Task SendEmailAsync(string email, string subject, string message);

        Task SendEmail(MailRequest request);

    }

}

