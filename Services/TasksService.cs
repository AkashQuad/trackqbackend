using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using server.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace server.Services
{
    public class TasksService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TasksService> _logger;

        public TasksService(
            IServiceProvider serviceProvider,
            ILogger<TasksService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Task Notification Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                var targetTimeToday = new DateTime(now.Year, now.Month, now.Day, 14, 40, 0);

                
                var timeUntilTarget = targetTimeToday > now
                    ? targetTimeToday - now
                    : targetTimeToday.AddDays(1) - now;

                _logger.LogInformation("Next task check scheduled in {hours} hours at {targetTime}",
                    timeUntilTarget.TotalHours, targetTimeToday);

                
                await Task.Delay(timeUntilTarget, stoppingToken);

                
                await ProcessTaskNotifications();
            }
        }

        private async Task ProcessTaskNotifications()
        {
            _logger.LogInformation("Processing task notifications at: {time}", DateTimeOffset.Now);

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                var currentDate = DateTime.Today;
                var tasksToNotify = await dbContext.Tasks
                    .Where(t => t.Status == "Not Started" || t.Status == "In Progress")
                    .Where(t => t.StartDate <= currentDate && (t.EndDate == null || t.EndDate >= currentDate))
                    .ToListAsync();

                _logger.LogInformation("Found {count} incomplete tasks for notification", tasksToNotify.Count);

                foreach (var task in tasksToNotify)
                {
                    
                    _logger.LogInformation(
                        "Notification needed for task {taskId}: {topic} (EmployeeId: {employeeId})",
                        task.TaskId, task.Topic, task.EmployeeId);

                    
                }

                
                foreach (var task in tasksToNotify.Where(t => t.EndDate < currentDate))
                {
                    task.Status = "Not Started";
                    task.Date = currentDate;
                    _logger.LogInformation(
                        "Reset overdue task {taskId} to Not Started for date {newDate}",
                        task.TaskId, task.Date.ToString("yyyy-MM-dd"));
                }

                if (tasksToNotify.Any())
                {
                    await dbContext.SaveChangesAsync();
                    _logger.LogInformation("Processed notifications for {count} tasks", tasksToNotify.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing task notifications: {message}", ex.Message);
            }
        }
    }
}