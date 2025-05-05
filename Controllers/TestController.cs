using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using server.Services;
using System;
using System.Threading.Tasks;

namespace server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly ILogger<TestController> _logger;
        private readonly ScheduledTaskService _scheduledTaskService;

        public TestController(
            ILogger<TestController> logger,
            ScheduledTaskService scheduledTaskService)
        {
            _logger = logger;
            _scheduledTaskService = scheduledTaskService;
        }

        [HttpGet("send-task-reminders")]
        public async Task<IActionResult> TestSendTaskReminders()
        {
            try
            {
                _logger.LogInformation("Manual trigger of task reminders");
                await _scheduledTaskService.SendTaskRemindersManuallyAsync();
                return Ok(new { message = "Task reminders sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending task reminders: {ex.Message}");
                return StatusCode(500, new { error = "Failed to send task reminders", details = ex.Message });
            }
        }
    }
}