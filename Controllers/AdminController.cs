using Microsoft.AspNetCore.Mvc;
using server.DTO;
using server.Model;
using server.Data;
using server.services;
using Microsoft.EntityFrameworkCore;
using BC = BCrypt.Net.BCrypt;
using System.Linq;
using NETCore.MailKit.Core;

namespace server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly server.services.IEmailService _emailService;

        public AdminController(ApplicationDbContext context, server.services.IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpGet("employees")]
        public async Task<IActionResult> GetEmployees(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string searchQuery = "")
        {
            var query = _context.Employees.Where(e => e.status).AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                query = query.Where(e =>
                    e.Username.Contains(searchQuery) ||
                    e.Email.Contains(searchQuery) ||
                    e.Role.RoleName.ToString().Contains(searchQuery));
            }

            var totalCount = await query.CountAsync();

            var employees = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new
                {
                    e.EmployeeId,
                    e.Username,
                    e.Email,
                    e.JoinedDate,
                    e.RoleID,
                    ManagerName = _context.Employees
                        .Where(m => m.EmployeeId == e.ManagerId)
                        .Select(m => m.Username)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(new
            {
                totalCount,
                pageCount = (int)Math.Ceiling(totalCount / (double)pageSize),
                currentPage = page,
                pageSize,
                employees
            });
        }

        [HttpGet("all-employees")]
        public async Task<IActionResult> GetAllEmployees()
        {
            var employees = await _context.Employees.Where(e => e.status)
                .Select(e => new
                {
                    e.EmployeeId,
                    e.Username,
                    e.Email,
                    e.JoinedDate,
                    e.RoleID,
                    ManagerName = _context.Employees
                        .Where(m => m.EmployeeId == e.ManagerId)
                        .Select(m => m.Username)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(employees);
        }

        [HttpDelete("delete-employee/{employeeId}")]
        public async Task<IActionResult> DeleteEmployee(int employeeId)
        {
            var employee = await _context.Employees.FindAsync(employeeId);

            if (employee == null)
                return NotFound("Employee not found!");

            if (!employee.status)
                return BadRequest("Employee is already deleted!");


            var subordinates = _context.Employees.Where(e => e.ManagerId == employeeId).ToList();

            foreach (var subordinate in subordinates)
            {
                subordinate.ManagerId = null;
            }

            employee.status = false;
            await _context.SaveChangesAsync();


            return Ok("Employee deleted successfully!");
        }



        [HttpDelete("hard-delete-employee/{employeeId}")]
        public async Task<IActionResult> HardDeleteEmployee(int employeeId)
        {
            var employee = await _context.Employees.FindAsync(employeeId);

            if (employee == null)
                return NotFound("Employee not found!");

            var subordinates = _context.Employees.Where(e => e.ManagerId == employeeId).ToList();

            foreach (var subordinate in subordinates)
            {
                subordinate.ManagerId = null;
            }

            await _context.SaveChangesAsync(); 

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync(); 
            return Ok("Employee permanently deleted successfully!");
        }




        [HttpPut("edit-employee/{employeeId}")]
        public async Task<IActionResult> EditEmployee(int employeeId, [FromBody] UpdateEmployeeDto updatedEmployee)
        {
            var employee = await _context.Employees.FindAsync(employeeId);

            if (employee == null)
            {
                return NotFound("Employee not found!");
            }


            if (!employee.status)
            {
                return BadRequest("Cannot edit a deleted employee!");
            }

           employee.Username = updatedEmployee.Username;
            employee.Email = updatedEmployee.Email;
            employee.RoleID = updatedEmployee.RoleId;
            employee.ManagerId = updatedEmployee.ManagerId;

            try
            {
                await _context.SaveChangesAsync();
                return Ok("Employee updated successfully!");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating employee: {ex.Message}");
            }
        }

        [HttpGet("get-managers")]
        public async Task<IActionResult> GetManagers()
        {
            var managers = await _context.Employees.Where(e => e.status)
                .Where(e => e.RoleID == 2) 
                .Select(e => new
                {
                    Id = e.EmployeeId,
                    Name = e.Username
                })
                .ToListAsync();

            if (managers == null || managers.Count == 0)
            {
                return NotFound("No managers found!");
            }

            return Ok(managers);
        }

        [HttpPost("batch-delete-employees")]
        public async Task<IActionResult> BatchDeleteEmployees([FromBody] List<int> employeeIds)
        {
            if (employeeIds == null || employeeIds.Count == 0)
                return BadRequest("No employee IDs provided!");

            var employeesToDelete = await _context.Employees
                                                  .Where(e => employeeIds.Contains(e.EmployeeId))
                                                  .ToListAsync();

            if (!employeesToDelete.Any())
                return NotFound("No employees found to delete!");

            var subordinateIds = await _context.Employees
                .Where(e => employeeIds.Contains(e.ManagerId ?? 0))
                .Select(e => e.EmployeeId)
                .ToListAsync();

            if (subordinateIds.Any())
            {
                var subordinates = await _context.Employees
                    .Where(e => subordinateIds.Contains(e.EmployeeId))
                    .ToListAsync();

                foreach (var subordinate in subordinates)
                {
                    subordinate.ManagerId = null;
                }


            }


            foreach (var employee in employeesToDelete)
            {
                employee.status = false;
            }

            await _context.SaveChangesAsync();

            return Ok("Employees deleted successfully!");
        }

        [HttpPut("batch-update-roles")]
        public async Task<IActionResult> BatchUpdateEmployeeRoles([FromBody] List<UpdateRoleDto> roleUpdates)
        {
            if (roleUpdates == null || roleUpdates.Count == 0)
                return BadRequest("No role updates provided!");

            var employeeIds = roleUpdates.Select(r => r.EmployeeId).ToList();
            var employees = await _context.Employees.Where(e => e.status).Where(e => employeeIds.Contains(e.EmployeeId)).ToListAsync();

            if (employees.Count == 0)
                return NotFound("No employees found to update roles!");

            foreach (var update in roleUpdates)
            {
                var employee = employees.FirstOrDefault(e => e.EmployeeId == update.EmployeeId);
                if (employee != null)
                {
                    if (employee.RoleID == 2 && update.RoleId != 2)
                    {
                        var usersUnderManager = await _context.Employees.Where(e => e.ManagerId == employee.EmployeeId).ToListAsync();
                        foreach (var user in usersUnderManager)
                        {
                            user.ManagerId = null;
                        }
                    }

                    employee.RoleID = update.RoleId;
                }
            }

            await _context.SaveChangesAsync();
            return Ok("Employee roles updated successfully!");
        }




        [HttpPost("batch-hard-delete-employees")]
        public async Task<IActionResult> BatchHardDeleteEmployees([FromBody] List<int> employeeIds)
        {
            if (employeeIds == null || employeeIds.Count == 0)
                return BadRequest("No employee IDs provided!");

            var employeesToDelete = await _context.Employees
                                                  .Where(e => employeeIds.Contains(e.EmployeeId))
                                                  .ToListAsync();

            if (!employeesToDelete.Any())
                return NotFound("No employees found to delete!");

            var subordinateIds = await _context.Employees
                .Where(e => employeeIds.Contains(e.ManagerId ?? 0))
                .Select(e => e.EmployeeId)
                .ToListAsync();

            if (subordinateIds.Any())
            {
                var subordinates = await _context.Employees
                    .Where(e => subordinateIds.Contains(e.EmployeeId))
                    .ToListAsync();

                foreach (var subordinate in subordinates)
                {
                    subordinate.ManagerId = null;
                }

                await _context.SaveChangesAsync();
            }

            _context.Employees.RemoveRange(employeesToDelete);
            await _context.SaveChangesAsync();

            return Ok("Employees permanently deleted successfully!");
        }





        [HttpPut("batch-update-managers")]
        public async Task<IActionResult> BatchUpdateEmployeeManagers([FromBody] List<UpdateManagerDto> managerUpdates)
        {
            if (managerUpdates == null || managerUpdates.Count == 0)
                return BadRequest("No manager updates provided!");

            var employeeIds = managerUpdates.Select(m => m.EmployeeId).ToList();
            var employees = await _context.Employees.Where(e => e.status).Where(e => employeeIds.Contains(e.EmployeeId)).ToListAsync();

            if (employees.Count == 0)
                return NotFound("No employees found to update managers!");

            foreach (var update in managerUpdates)
            {
                var employee = employees.FirstOrDefault(e => e.EmployeeId == update.EmployeeId);
                if (employee != null)
                {
                    employee.ManagerId = update.ManagerId; 
                }
            }

            await _context.SaveChangesAsync();
            return Ok("Employee managers updated successfully!");
        }


        [HttpGet("deleted-employees")]
        public async Task<IActionResult> GetDeletedEmployees(
          [FromQuery] int page = 1,
          [FromQuery] int pageSize = 10,
          [FromQuery] string searchQuery = "")
        {
            var query = _context.Employees
                        .Where(e => !e.status) 
                        .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                query = query.Where(e =>
                    e.Username.Contains(searchQuery) ||
                    e.Email.Contains(searchQuery) ||
                    e.Role.RoleName.ToString().Contains(searchQuery));
            }

            var totalCount = await query.CountAsync();

            var employees = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new
                {
                    e.EmployeeId,
                    e.Username,
                    e.Email,
                    e.JoinedDate,
                    e.RoleID,
                    ManagerName = _context.Employees
                        .Where(m => m.EmployeeId == e.ManagerId)
                        .Select(m => m.Username)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(new
            {
                totalCount,
                pageCount = (int)Math.Ceiling(totalCount / (double)pageSize),
                currentPage = page,
                pageSize,
                employees
            });
        }

        [HttpPut("restore-employee/{employeeId}")]
        public async Task<IActionResult> RestoreEmployee(int employeeId)
        {
            var employee = await _context.Employees.FindAsync(employeeId);

            if (employee == null)
                return NotFound("Employee not found!");

            if (employee.status)
                return BadRequest("Employee is not deleted!");

            employee.status = true;
            await _context.SaveChangesAsync();

            return Ok("Employee restored successfully!");
        }

        [HttpPost("batch-restore-employees")]
        public async Task<IActionResult> BatchRestoreEmployees([FromBody] List<int> employeeIds)
        {
            if (employeeIds == null || employeeIds.Count == 0)
                return BadRequest("No employee IDs provided!");

            var employeesToRestore = await _context.Employees
                                                   .Where(e => employeeIds.Contains(e.EmployeeId) && !e.status)
                                                   .ToListAsync();

            if (!employeesToRestore.Any())
                return NotFound("No deleted employees found to restore!");

            foreach (var employee in employeesToRestore)
            {
                employee.status = true;
            }

            await _context.SaveChangesAsync();

            return Ok("Employees restored successfully!");
        }






        [HttpPost("batch-insert")]
        public async Task<IActionResult> BatchInsertEmployees([FromBody] List<EmployeeDTO> employees)
        {
            if (employees == null || !employees.Any())
                return BadRequest("No employee data provided!");

            var existingEmails = await _context.Employees.Select(e => e.Email).ToListAsync();
            var newEmployees = new List<Employee>();

            foreach (var employeeDto in employees)
            {
                if (!existingEmails.Contains(employeeDto.Email))
                {
                    var employee = new Employee
                    {
                        Username = employeeDto.Username,
                        Email = employeeDto.Email,
                        RoleID = employeeDto.RoleID,
                        ManagerId = employeeDto.ManagerId,
                        JoinedDate = DateTime.UtcNow,
                        status = true
                    };
                    newEmployees.Add(employee);
                }
            }

            if (!newEmployees.Any())
                return BadRequest("No new employees to add.");

            _context.Employees.AddRange(newEmployees);
            await _context.SaveChangesAsync();

            // Send emails to new employees
            foreach (var employee in newEmployees)
            {
                var mailRequest = new MailRequest
                {
                    Email = employee.Email,
                    Subject = "Account Created - Change Your Password",
                    Emailbody = $"Dear {employee.Username},\n\nYour account has been created. Please change your password using the following link:\n\n[http://localhost:5174/forgot-password]\n\nBest regards,\nYour Company"
                };
                await _emailService.SendEmailAsync(mailRequest.Email, mailRequest.Subject, mailRequest.Emailbody);
            }

            return Ok("Employees added successfully!");
        }

        [HttpPost("create-employee")]
        public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeDto newEmployee)
        {
            // Validate input
            if (newEmployee == null)
                return BadRequest("Employee information is required.");

            // Check if   email already exists


            if (await _context.Employees.AnyAsync(e => e.Email == newEmployee.Email))
                return Conflict($"Email {newEmployee.Email} is already in use.");

            // Create new employee
            var employee = new Employee
            {
                Username = newEmployee.Username,
                Email = newEmployee.Email,
                RoleID = newEmployee.RoleId,
                ManagerId = newEmployee.ManagerId,
                JoinedDate = DateTime.UtcNow,

                status = true
            };

            try
            {
                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetEmployeeDetails), new { employeeId = employee.EmployeeId }, employee);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error creating employee: {ex.Message}");
            }
        }

        [HttpGet("employee-details/{employeeId}")]
        public async Task<IActionResult> GetEmployeeDetails(int employeeId)
        {
            var employee = await _context.Employees.Where(e => e.status)
                .Include(e => e.Role)
                .Select(e => new
                {
                    e.EmployeeId,
                    e.Username,
                    e.Email,
                    e.JoinedDate,
                    Role = e.Role.RoleName,
                    e.status,
                    ManagerName = _context.Employees
                        .Where(m => m.EmployeeId == e.ManagerId)
                        .Select(m => m.Username)
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

            if (employee == null)
                return NotFound($"Employee with ID {employeeId} not found.");

            return Ok(employee);
        }

        [HttpGet("search-employees")]
        public async Task<IActionResult> SearchEmployees([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Search query cannot be empty!");

            var employees = await _context.Employees
                .Where(e => e.status && e.Username.Contains(query) || e.Email.Contains(query) || e.Role.RoleName.Contains(query))
                .Select(e => new
                {
                    e.EmployeeId,
                    e.Username,
                    e.Email,
                    e.JoinedDate,
                    e.RoleID,
                    ManagerName = _context.Employees
                        .Where(m => m.EmployeeId == e.ManagerId)
                        .Select(m => m.Username)
                        .FirstOrDefault()
                })
                .ToListAsync();

            if (!employees.Any())
                return NotFound("No employees matched the search criteria!");

            return Ok(employees);
        }
    }
}
