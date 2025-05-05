using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using server.Data;
using server.services;
using server.Services;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace server.Services
{
    public class ScheduledTaskService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ScheduledTaskService> _logger;

        public ScheduledTaskService(
            IServiceProvider serviceProvider,
            ILogger<ScheduledTaskService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Scheduled Task Service running.");

            while (!stoppingToken.IsCancellationRequested)
            {
                // Get current time
                var now = DateTime.Now;

                // Calculate time until next 5 PM
                var nextRun = new DateTime(now.Year, now.Month, now.Day, 12, 53, 0);
                if (now > nextRun)
                {
                    nextRun = nextRun.AddDays(1);
                }

                var delay = nextRun - now;
                _logger.LogInformation($"Next task reminder scheduled for: {nextRun}, waiting for {delay.TotalHours:F1} hours");

                // Wait until next run time
                await Task.Delay(delay, stoppingToken);

                // Execute the job
                await SendTaskRemindersInternalAsync();
            }
        }

        private async Task SendTaskRemindersInternalAsync()
        {
            // Create a new scope to resolve scoped services
            using (var scope = _serviceProvider.CreateScope())
            {
                try
                {
                    await SendTaskRemindersAsync(scope.ServiceProvider);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error in scheduled task: {ex.Message}");
                }
            }
        }

        // Public method for manual testing
        public async Task SendTaskRemindersManuallyAsync()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                await SendTaskRemindersAsync(scope.ServiceProvider);
            }
        }

        private async Task SendTaskRemindersAsync(IServiceProvider serviceProvider)
        {
            _logger.LogInformation("Sending task update reminders to users...");

            try
            {
                var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
                var emailService = serviceProvider.GetRequiredService<IEmailService>();

                // Find all users with role = "User" (RoleID = 1 based on your seeding)
                var users = await dbContext.Employees
                    .Where(e => e.RoleID == 1) // Role ID for "User"
                    .ToListAsync();

                _logger.LogInformation($"Found {users.Count} users to send reminders to");

                foreach (var user in users)
                {
                    try
                    {
                        // Fetch tasks for the user
                        var tasks = await dbContext.Tasks
                            .Where(t => t.EmployeeId == user.EmployeeId)
                            .ToListAsync();

                        // Prepare task details for the email body
                        var taskDetails = tasks.Select(t => $@"
                            <p>Task ID: {t.TaskId}</p>
                            <p>Topic: {t.Topic}</p>
                            <p>Status: {t.Status}</p>
                            <p>Date: {t.Date.ToShortDateString()}</p>
                            <p>Description: {t.Description}</p>
                            <hr/>
                ").Aggregate((current, next) => current + next);

                        // Prepare email content
                        var mailRequest = new Model.MailRequest
                        {
                            Email = user.Email,
                            Subject = "Daily Task Update Reminder",
                            Emailbody = $@"
                                <html>
                                <body>
                                <h2>Daily Task Update Reminder</h2>
                                <p>Hello {user.Username},</p>
                                <p>Please update your tasks for today. Here are your current tasks:</p>
                                                        {taskDetails}
                                <p>Thank you!</p>
                                </body>
                                </html>"
                        };

                        // Send the email
                        await emailService.SendEmail(mailRequest);
                        _logger.LogInformation($"Task reminder sent to {user.Email}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Failed to send reminder to {user.Email}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending task reminders: {ex.Message}");
                throw;
            }
        }
    }
}