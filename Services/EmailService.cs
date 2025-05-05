

using Microsoft.Extensions.Options;

using server.Model;

using MimeKit;

using MailKit.Security;

using MailKit.Net.Smtp;

using server.services;

namespace server.Services

{

    public class EmailService : IEmailService

    {

        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> options)

        {

            _emailSettings = options.Value;

            Console.WriteLine($"Email Service initialized with: {_emailSettings.Email}");

            Console.WriteLine($"Display Name: {_emailSettings.DisplayName}");

            Console.WriteLine($"Host: {_emailSettings.Host}");

        }

        public async Task SendEmail(MailRequest request)

        {

            var email = new MimeMessage();

            email.Sender = new MailboxAddress(_emailSettings.DisplayName, _emailSettings.Email);

            email.From.Add(new MailboxAddress(_emailSettings.DisplayName, _emailSettings.Email));

            email.To.Add(MailboxAddress.Parse(request.Email));

            email.Subject = request.Subject;

            var builder = new BodyBuilder { HtmlBody = request.Emailbody };

            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();

            await smtp.ConnectAsync(_emailSettings.Host, _emailSettings.Port, SecureSocketOptions.StartTls);

            await smtp.AuthenticateAsync(_emailSettings.Email, _emailSettings.Password);

            await smtp.SendAsync(email);

            await smtp.DisconnectAsync(true);

        }

        public async Task SendEmailAsync(string email, string subject, string message)

        {

            var emailMessage = new MimeMessage();

            emailMessage.From.Add(new MailboxAddress(_emailSettings.DisplayName, _emailSettings.Email));

            emailMessage.To.Add(new MailboxAddress("", email));

            emailMessage.Subject = subject;

            emailMessage.Body = new TextPart("plain") { Text = message };

            using var smtp = new SmtpClient();

            await smtp.ConnectAsync(_emailSettings.Host, _emailSettings.Port, SecureSocketOptions.StartTls);

            await smtp.AuthenticateAsync(_emailSettings.Email, _emailSettings.Password);

            await smtp.SendAsync(emailMessage);

            await smtp.DisconnectAsync(true);

        }

    }

}

