using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Data;
using server.DTO;
using server.Model;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ManagerController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ManagerController(ApplicationDbContext context)
        {
            _context = context;
        }

        //// Get all tasks for employees reporting to the manager
        //[HttpGet("tasks")]
        //public async Task<IActionResult> GetTasksForEmployees(int managerId)
        //{
        //    var tasks = await _context.Tasks
        //        .Where(t => _context.Employees.Any(e => e.EmployeeId == t.EmployeeId && e.ManagerId == managerId))
        //        .ToListAsync();

        //    return Ok(tasks);
        //}
        [HttpGet("GetTasksByManagerAndDate/{managerId}/{date}")]
        public async Task<IActionResult> GetTasksByManagerAndDate(int managerId, DateTime date)


        {
            DateTime parsedDate = date.Date;


            // Retrieve tasks assigned to employees managed by the given manager and for the given date
            var tasks = await _context.Tasks
       .Where(t => t.EmployeeId != null && _context.Employees.Any(e => e.EmployeeId == t.EmployeeId && e.ManagerId == managerId) && t.Date.Date == parsedDate)
    
       .ToListAsync();

            if (tasks == null || tasks.Count == 0)
            {
                return NotFound($"No tasks found for Manager ID: {managerId} on {date.ToShortDateString()}");
            }

            // Map tasks to DTOs
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
                Status = task.Status
            }).ToList();

            return Ok(tasksDTO);
        }






        // Get details of employees reporting to a specific manager
        [HttpGet("{managerId}/employees")]
        public async Task<IActionResult> GetEmployeesReportingToManager(int managerId)
        {
            var employees = await _context.Employees
                .Where(e => e.ManagerId == managerId)
                .Select(e => new EmployeeDTO
                {
                    EmployeeId = e.EmployeeId,
                    Username = e.Username,
                    Email = e.Email,
                    JoinedDate = e.JoinedDate,
                    RoleID = e.RoleID,
                    Role = e.Role.RoleName
                })
                .ToListAsync();

            return Ok(employees);
        }






        // Get details of a specific employee
        [HttpGet("employee/{id}")]
        public async Task<IActionResult> GetEmployeeDetails(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.Role)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee == null)
            {
                return NotFound();
            }

            var employeeDto = new EmployeeDTO
            {
                EmployeeId = employee.EmployeeId,
                Username = employee.Username,
                Email = employee.Email,
                JoinedDate = employee.JoinedDate, 
                RoleID = employee.RoleID,
                Role = employee.Role.RoleName
            };

            return Ok(employeeDto);
        }

        // Search for employees by name
        [HttpGet("search")]
        public async Task<IActionResult> SearchEmployees(string name, int managerId)
        {
            var employees = await _context.Employees
                .Where(e => e.ManagerId == managerId && e.Username.Contains(name))
                .Select(e => new EmployeeDTO
                {
                    EmployeeId = e.EmployeeId,
                    Username = e.Username,
                    Email = e.Email,
                    JoinedDate = e.JoinedDate,
                    RoleID = e.RoleID,
                    Role = e.Role.RoleName
                })
                .ToListAsync();

            return Ok(employees);
        }

        [HttpPost("assign")]
        public async Task<IActionResult> AssignTaskToEmployee([FromBody] TasksDTO taskAssignment)
        {
            // Verify the manager exists
            var manager = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == taskAssignment.AssignedBy && e.RoleID == 2);

            if (manager == null)
            {
                return BadRequest($"Manager with ID {taskAssignment.AssignedBy} not found or doesn't have manager role.");
            }

            // Verify the employee exists
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == taskAssignment.EmployeeId);

            if (employee == null)
            {
                return NotFound($"Employee with ID {taskAssignment.EmployeeId} not found.");
            }

            // Create the new task
            var newTask = new Tasks
            {
                EmployeeId = taskAssignment.EmployeeId,
                Topic = taskAssignment.Topic,
                SubTopic = taskAssignment.SubTopic,
                Description = taskAssignment.Description,
                Date = taskAssignment.Date,
                StartDate = taskAssignment.StartDate,
                EndDate = taskAssignment.EndDate,
                CompletedHours = 0, // Initially zero as task is just being assigned
                ExpectedHours = taskAssignment.ExpectedHours,
                priority = taskAssignment.priority,
                Status = taskAssignment.Status, // Initial status
                AssignedBy = taskAssignment.AssignedBy, // Take manager ID from request
                AssignedDate = DateTime.UtcNow, // Track when the task was assigned
                AssignedManager = taskAssignment.AssignedManager
               
            };

            _context.Tasks.Add(newTask);
            await _context.SaveChangesAsync();

            // Return detailed information about the assigned task
            var assignedTaskDetails = new
            {
                TaskId = newTask.TaskId,
                EmployeeId = newTask.EmployeeId,
                EmployeeName = employee.Username,
                EmployeeEmail = employee.Email,
                Topic = newTask.Topic,
                SubTopic = newTask.SubTopic,
                Description = newTask.Description,
                StartDate = newTask.StartDate,
                EndDate = newTask.EndDate,
                ExpectedHours = newTask.ExpectedHours,
                Priority = newTask.priority,
                Status = newTask.Status,
                AssignedBy = manager.Username,
                AssignedDate = newTask.AssignedDate,
                AssignedManager = newTask.AssignedManager
            };

            return CreatedAtAction("GetTaskById", "Tasks", new { id = newTask.TaskId }, assignedTaskDetails);
        }
    }
}