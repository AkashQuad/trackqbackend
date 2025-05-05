using Microsoft.AspNetCore.Http;

using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;

using server.Data;

using server.DTO;

namespace server.Controllers

{

    [Route("api/[controller]")]

    [ApiController]

    public class UserController : ControllerBase

    {

        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)

        {

            _context = context;

        }


        [HttpGet("{id}")]

        public async Task<IActionResult> GetUser(int id)

        {

            var user = await _context.Employees.FindAsync(id);

            if (user == null)

                return NotFound("User not found!");

            var userDto = new EmployeeDTO

            {

                EmployeeId = user.EmployeeId,

                Username = user.Username,

                Email = user.Email,

                JoinedDate = user.JoinedDate,

                RoleID = user.RoleID,

                Role = user.Role?.RoleName 

            };

            return Ok(userDto);


        }


        [HttpGet]

        public async Task<IActionResult> GetAllEmployees()

        {

            var employees = await _context.Employees

                .Select(user => new EmployeeDTO

                {

                    EmployeeId = user.EmployeeId,

                    Username = user.Username,


                })

                .ToListAsync();

            return Ok(employees);

        }



    }

}

