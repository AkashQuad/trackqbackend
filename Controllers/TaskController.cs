using Microsoft.AspNetCore.Mvc;
using server.DTO;
using server.Model;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using server.Data;
using Microsoft.Extensions.Logging;

namespace server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TasksController> _logger;

        public TasksController(ApplicationDbContext context, ILogger<TasksController> logger)
        {
            _context = context;
            _logger = logger;
        }

        



        [HttpPost]
        public async Task<IActionResult> CreateTask([FromBody] TasksDTO newTaskDTO)
        {
            if (string.IsNullOrEmpty(newTaskDTO.Status))
            {
                newTaskDTO.Status = "Not Started";
            }

            var newTask = new Tasks
            {
                EmployeeId = newTaskDTO.EmployeeId,
                Topic = newTaskDTO.Topic,
                SubTopic = newTaskDTO.SubTopic,
                Description = newTaskDTO.Description,
                Date = newTaskDTO.Date,
                StartDate = newTaskDTO.StartDate,
                EndDate = newTaskDTO.EndDate,
                CompletedHours = newTaskDTO.CompletedHours,
                ExpectedHours = newTaskDTO.ExpectedHours,
                priority = newTaskDTO.priority,
                Status = newTaskDTO.Status
            };

            _context.Tasks.Add(newTask);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new task with ID {taskId} and status {status}",
                newTask.TaskId, newTask.Status);

            return CreatedAtAction(nameof(GetTaskById), new { id = newTask.TaskId }, newTaskDTO);
        }





        [HttpPost("rollover")]
        public async Task<IActionResult> ManualRollover()
        {
            try
            {
                var currentDate = DateTime.Now.Date;
                var tasksToUpdate = await _context.Tasks
                    .Where(t => (t.Status == "Not Started" || t.Status == "In Progress" || t.Status == "Pending") &&
                                t.Date.Date <= currentDate)
                    .ToListAsync();

                if (!tasksToUpdate.Any())
                {
                    return Ok(new { message = "No tasks found that need to be rolled over" });
                }

                foreach (var task in tasksToUpdate)
                {
                    var previousDate = task.Date;
                    task.Date = task.Date.AddDays(1);

                    _logger.LogInformation("Manually rolling over task {taskId} from {oldDate} to {newDate}",
                        task.TaskId, previousDate.ToString("yyyy-MM-dd"), task.Date.ToString("yyyy-MM-dd"));
                }

                await _context.SaveChangesAsync();
                return Ok(new
                {
                    message = $"Successfully rolled over {tasksToUpdate.Count} tasks",
                    tasksUpdated = tasksToUpdate.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during manual rollover");
                return StatusCode(500, new { message = "An error occurred during rollover", error = ex.Message });
            }
        }











        [HttpPut("{id}")]
        public async Task<IActionResult> GetTaskById(int id, [FromBody] TasksDTO updatedTaskDTO)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
            {
                return NotFound("Task not found");
            }

            string currentStatus = task.Status;
            string newStatus = updatedTaskDTO.Status;

            if (currentStatus != newStatus)
            {
                if (currentStatus == "Not Started" && newStatus == "Completed")
                {
                    return BadRequest("Cannot change status directly from 'Not Started' to 'Completed'. Please move to 'In Progress' first.");
                }
            }

            task.EmployeeId = updatedTaskDTO.EmployeeId;
            task.Topic = updatedTaskDTO.Topic;
            task.SubTopic = updatedTaskDTO.SubTopic;
            task.Description = updatedTaskDTO.Description;

            
            if (newStatus == "Completed" && currentStatus != "Completed")
            {
                
                task.EndDate = DateTime.Now; 
                _logger.LogInformation("Task {taskId} marked as completed on date {date}",
                    task.TaskId, task.Date.ToString("yyyy-MM-dd"));
            }
            else
            {
                if (updatedTaskDTO.Date.Date >= task.Date.Date)
                {
                    task.Date = updatedTaskDTO.Date;
                }
                else
                {
                    _logger.LogWarning("Attempted to move task {taskId} back in time from {oldDate} to {newDate}. Keeping original date.",
                        task.TaskId, task.Date.ToString("yyyy-MM-dd"), updatedTaskDTO.Date.ToString("yyyy-MM-dd"));
                }

                task.StartDate = updatedTaskDTO.StartDate;
                task.EndDate = updatedTaskDTO.EndDate;
            }

            task.CompletedHours = updatedTaskDTO.CompletedHours;
            task.ExpectedHours = updatedTaskDTO.ExpectedHours;
            task.priority = updatedTaskDTO.priority;
            task.Status = updatedTaskDTO.Status;

            // Log the status change
            if (currentStatus != newStatus)
            {
                _logger.LogInformation("Changed task {taskId} status from {oldStatus} to {newStatus}",
                    task.TaskId, currentStatus, newStatus);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Task updated successfully", task = task });
        }


        [HttpGet("search")]
        public async Task<IActionResult> GetTasksByDateAndStatus([FromQuery] DateTime date, [FromQuery] string status)
        {
            var filteredTasks = await _context.Tasks
                .Where(t => t.Date.Date == date.Date && t.Status == status)
                .ToListAsync();

            var filteredTasksDTO = filteredTasks.Select(task => new TasksDTO
            {
                TaskId = task.TaskId,
                EmployeeId = task.EmployeeId,
                Topic = task.Topic,
                SubTopic = task.SubTopic,
                Description = task.Description,
                Date = task.Date,
                CompletedHours = task.CompletedHours,
                ExpectedHours = task.ExpectedHours,
                StartDate = task.StartDate,
                EndDate = task.EndDate,
                Status = task.Status
            }).ToList();

            return Ok(filteredTasksDTO);
        }

        [HttpGet("task/{taskId}/employee/{employeeId}")]
        public async Task<IActionResult> GetTaskByTaskIdAndEmployeeId(int taskId, int employeeId)
        {
            var task = await _context.Tasks
                .FirstOrDefaultAsync(t => t.TaskId == taskId && t.EmployeeId == employeeId);
            if (task == null)
            {
                return NotFound();
            }
            return Ok(task);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllTasks()
        {
            var tasks = await _context.Tasks.ToListAsync();

            var sortedTasks = tasks.OrderBy(task => task.priority).ToList();

            var tasksDTO = sortedTasks.Select(task => new TasksDTO
            {
                TaskId = task.TaskId,
                EmployeeId = task.EmployeeId,
                Topic = task.Topic,
                SubTopic = task.SubTopic,
                Description = task.Description,
                Date = task.Date,
                CompletedHours = task.CompletedHours,
                priority = task.priority,
                ExpectedHours = task.ExpectedHours,
                StartDate = task.StartDate,
                EndDate = task.EndDate,
                Status = task.Status
            }).ToList();

            return Ok(tasksDTO);
        }

        [HttpGet("employee/{employeeId}")]
        public async Task<IActionResult> GetTasksByEmployeeId(int employeeId)
        {
            var employeeTasks = await _context.Tasks
                .Where(t => t.EmployeeId == employeeId)
                .ToListAsync();
            if (!employeeTasks.Any())
            {
                return NotFound();
            }

            var tasks = await _context.Tasks.ToListAsync();

            var sortedTasks = employeeTasks.OrderBy(task => task.priority).ToList();

            var tasksDTO = sortedTasks.Select(task => new TasksDTO
            {
                TaskId = task.TaskId,
                EmployeeId = task.EmployeeId,
                Topic = task.Topic,
                SubTopic = task.SubTopic,
                Description = task.Description,
                Date = task.Date,
                CompletedHours = task.CompletedHours,
                priority = task.priority,
                ExpectedHours = task.ExpectedHours,
                StartDate = task.StartDate,
                EndDate = task.EndDate,
                Status = task.Status,
                AssignedBy = task.AssignedBy
            }).ToList();

            return Ok(tasksDTO);
        }

        [HttpGet("details")]

        public async Task<IActionResult> GetTaskDetailsByDateAndEmployeeId([FromQuery] DateTime dateQ, [FromQuery] int employeeId)

        {
            dateQ = dateQ.Date;

            var tasks = await _context.Tasks

                .Where(t => t.Date.Date == dateQ.Date && t.EmployeeId == employeeId)

                .Join(_context.Employees,

                      task => task.EmployeeId,

                      user => user.EmployeeId,

                      (task, user) => new

                      {

                          TaskId = task.TaskId,

                          EmployeeId = task.EmployeeId,

                          EmployeeName = user.Username,

                          EmployeeEmail = user.Email,

                          Topic = task.Topic,

                          SubTopic = task.SubTopic,

                          Description = task.Description,

                          Date = task.Date,

                          CompletedHours = task.CompletedHours,

                          priority = task.priority,
                          ExpectedHours = task.ExpectedHours,
                          StartDate = task.StartDate,
                          EndDate = task.EndDate,

                          Status = task.Status

                      })

                .ToListAsync();

            if (!tasks.Any())
            {
                return Ok(new List<object>());
            }

            return Ok(tasks);

        }



        [HttpDelete("{taskId}")]

        public async Task<IActionResult> DeleteTask(int taskId)

        {

            var task = await _context.Tasks.FindAsync(taskId);

            if (task == null)

            {

                return NotFound("Task not found!");

            }

            _context.Tasks.Remove(task);

            await _context.SaveChangesAsync();

            return Ok("Task deleted successfully!");

        }

        [HttpGet("employee/{employeeId}/private")]
        public async Task<IActionResult> GetPrivateTasksByEmployeeId(int employeeId)
        {
            var privateTasks = await _context.Tasks
                .Where(t => t.EmployeeId == employeeId && t.AssignedBy == null)
                .OrderBy(t => t.priority)
                .ToListAsync();

            if (!privateTasks.Any())
            {
                return Ok(new List<TasksDTO>()); 
            }

            var tasksDTO = privateTasks.Select(task => new TasksDTO
            {
                TaskId = task.TaskId,
                EmployeeId = task.EmployeeId,
                Topic = task.Topic,
                SubTopic = task.SubTopic,
                Description = task.Description,
                Date = task.Date,
                CompletedHours = task.CompletedHours,
                priority = task.priority,
                ExpectedHours = task.ExpectedHours,
                StartDate = task.StartDate,
                EndDate = task.EndDate,
                Status = task.Status,
                AssignedBy = task.AssignedBy
            }).ToList();

            _logger.LogInformation("Retrieved {count} private tasks for employee {employeeId}",
                privateTasks.Count, employeeId);

            return Ok(tasksDTO);
        }

        [HttpGet("employee/{employeeId}/assigned")]
        public async Task<IActionResult> GetAssignedTasksByEmployeeId(int employeeId)
        {
            var assignedTasks = await _context.Tasks
                .Where(t => t.EmployeeId == employeeId && t.AssignedBy != null)
                .OrderBy(t => t.priority)
                .ToListAsync();

            if (!assignedTasks.Any())
            {
                return Ok(new List<TasksDTO>());
            }

            var tasksDTO = assignedTasks.Select(task => new TasksDTO
            {
                TaskId = task.TaskId,
                EmployeeId = task.EmployeeId,
                Topic = task.Topic,
                SubTopic = task.SubTopic,
                Description = task.Description,
                Date = task.Date,
                CompletedHours = task.CompletedHours,
                priority = task.priority,
                ExpectedHours = task.ExpectedHours,
                StartDate = task.StartDate,
                EndDate = task.EndDate,
                Status = task.Status,
                AssignedBy = task.AssignedBy
            }).ToList();

            _logger.LogInformation("Retrieved {count} assigned tasks for employee {employeeId}",
                assignedTasks.Count, employeeId);

            return Ok(tasksDTO);
        }



        [HttpPost("{taskId}/hours")]
        public async Task<IActionResult> UpdateDailyHours(int taskId, [FromBody] DailyHoursInput input)
        {
            var task = await _context.Tasks.FindAsync(taskId);
            if (task == null)
                return NotFound();

            var today = DateTime.Today;
            var existingHours = await _context.DailyTaskHours
                .FirstOrDefaultAsync(h => h.TaskId == taskId && h.Date == today);

            if (existingHours == null)
            {
                existingHours = new DailyTaskHours
                {
                    TaskId = taskId,
                    Date = today,
                    HoursSpent = (int)input.HoursSpent
                };
                _context.DailyTaskHours.Add(existingHours);
            }
            else
            {
                existingHours.HoursSpent = (int)input.HoursSpent;
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("employee/{employeeId}/incomplete")]
        public async Task<ActionResult<IEnumerable<Task>>> GetIncompleteTasks(int employeeId)
        {
            var today = DateTime.Today;
            var tasks = await _context.Tasks
                .Where(t => t.EmployeeId == employeeId)
                .Where(t => t.Status == "Not Started" || t.Status == "In Progress")
                .Where(t => t.StartDate <= today && (t.EndDate == null || t.EndDate >= today))
                .ToListAsync();

            return Ok(tasks);
        }


        [HttpGet("{taskId}/daily-hours")]
        public async Task<IActionResult> GetDailyHours(int taskId)
        {
            var task = await _context.Tasks.FindAsync(taskId);
            if (task == null)
                return NotFound("Task not found.");

            var dailyHours = await _context.DailyTaskHours
                .Where(h => h.TaskId == taskId)
                .Select(h => new DailyTaskHoursDto
                {
                    Date = h.Date,
                    HoursSpent = h.HoursSpent
                })
                .OrderBy(h => h.Date) 
                .ToListAsync();

            return Ok(dailyHours);
        }


        [HttpGet("status/overdue")]
        public async Task<IActionResult> GetOverdueTasks([FromQuery] int? employeeId)
        {
            var currentDate = DateTime.Today;
            var query = _context.Tasks
                .Where(t => t.Status == "Overdue" ||
                           (t.EndDate != null && t.EndDate < currentDate &&
                            t.Status != "Completed"));

            if (employeeId.HasValue)
            {
                query = query.Where(t => t.EmployeeId == employeeId.Value);
            }

            var overdueTasks = await query
                .OrderBy(task => task.priority)
                .ToListAsync();

            var tasksDTO = overdueTasks.Select(task => new TasksDTO
            {
                TaskId = task.TaskId,
                EmployeeId = task.EmployeeId,
                Topic = task.Topic,
                SubTopic = task.SubTopic,
                Description = task.Description,
                Date = task.Date,
                CompletedHours = task.CompletedHours,
                priority = task.priority,
                ExpectedHours = task.ExpectedHours,
                StartDate = task.StartDate,
                EndDate = task.EndDate,
                Status = task.Status,
                AssignedBy = task.AssignedBy
            }).ToList();

            _logger.LogInformation("Retrieved {count} overdue tasks for employee {employeeId}",
                overdueTasks.Count, employeeId.HasValue ? employeeId.Value.ToString() : "all");

            return Ok(tasksDTO);
        }

        [HttpGet("status/active")]
        public async Task<IActionResult> GetActiveTasks([FromQuery] int? employeeId)
        {
            var currentDate = DateTime.Today;
            var query = _context.Tasks
                .Where(t => (t.Status == "Not Started" || t.Status == "In Progress") &&
                           t.StartDate <= currentDate &&
                           (t.EndDate == null || t.EndDate >= currentDate));

            if (employeeId.HasValue)
            {
                query = query.Where(t => t.EmployeeId == employeeId.Value);
            }

            var activeTasks = await query
                .OrderBy(task => task.priority)
                .ToListAsync();

            var tasksDTO = activeTasks.Select(task => new TasksDTO
            {
                TaskId = task.TaskId,
                EmployeeId = task.EmployeeId,
                Topic = task.Topic,
                SubTopic = task.SubTopic,
                Description = task.Description,
                Date = task.Date,
                CompletedHours = task.CompletedHours,
                priority = task.priority,
                ExpectedHours = task.ExpectedHours,
                StartDate = task.StartDate,
                EndDate = task.EndDate,
                Status = task.Status,
                AssignedBy = task.AssignedBy
            }).ToList();

            _logger.LogInformation("Retrieved {count} active tasks for employee {employeeId}",
                activeTasks.Count, employeeId.HasValue ? employeeId.Value.ToString() : "all");

            return Ok(tasksDTO);
        }

        [HttpPost("update-overdue")]
        public async Task<IActionResult> UpdateOverdueTasks()
        {
            try
            {
                var currentDate = DateTime.Today;
                var tasksToUpdate = await _context.Tasks
                    .Where(t => (t.Status == "Not Started" || t.Status == "In Progress") &&
                                t.EndDate != null && t.EndDate < currentDate)
                    .ToListAsync();

                if (!tasksToUpdate.Any())
                {
                    return Ok(new { message = "No overdue tasks found" });
                }

                foreach (var task in tasksToUpdate)
                {
                    task.Status = "Overdue";
                    _logger.LogInformation("Task {taskId} marked as overdue (due date: {dueDate})",
                        task.TaskId, task.EndDate.ToString("yyyy-MM-dd"));
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = $"Marked {tasksToUpdate.Count} tasks as overdue" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating overdue tasks");
                return StatusCode(500, new { message = "An error occurred updating overdue tasks", error = ex.Message });
            }
        }

        [HttpGet("status/{status}")]
        public async Task<IActionResult> GetTasksByStatus(string status)
        {
            var tasks = await _context.Tasks
                .Where(t => t.Status == status)
                .OrderBy(t => t.priority)
                .ToListAsync();

            var tasksDTO = tasks.Select(task => new TasksDTO
            {
                TaskId = task.TaskId,
                EmployeeId = task.EmployeeId,
                Topic = task.Topic,
                SubTopic = task.SubTopic,
                Description = task.Description,
                Date = task.Date,
                CompletedHours = task.CompletedHours,
                priority = task.priority,
                ExpectedHours = task.ExpectedHours,
                StartDate = task.StartDate,
                EndDate = task.EndDate,
                Status = task.Status,
                AssignedBy = task.AssignedBy
            }).ToList();

            _logger.LogInformation("Retrieved {count} tasks with status '{status}'", tasks.Count, status);

            return Ok(tasksDTO);
        }
    }

    public class DailyHoursInput
    {
        public decimal HoursSpent { get; set; }
    }



}