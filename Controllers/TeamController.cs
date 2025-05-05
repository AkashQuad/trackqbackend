using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Data;
using server.DTO;
using server.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TeamController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TeamController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Create a new team
        [HttpPost("create")]
        public async Task<IActionResult> CreateTeam([FromBody] CreateTeamDTO createTeamDTO)
        {
            var manager = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == createTeamDTO.ManagerId && e.RoleID == 2);

            if (manager == null)
            {
                return BadRequest($"Manager with ID {createTeamDTO.ManagerId} not found or doesn't have manager role.");
            }

            var employeeIds = createTeamDTO.MemberIds;
            var employees = await _context.Employees
                .Where(e => employeeIds.Contains(e.EmployeeId))
                .ToListAsync();

            if (employees.Count != employeeIds.Count)
            {
                return BadRequest("One or more employees do not exist.");
            }

            var team = new Team
            {
                TeamName = createTeamDTO.TeamName,
                Description = createTeamDTO.Description,
                ManagerId = createTeamDTO.ManagerId,
                CreatedDate = DateTime.UtcNow
            };

            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            foreach (var employeeId in employeeIds)
            {
                var teamMember = new TeamMember
                {
                    TeamId = team.TeamId,
                    EmployeeId = employeeId,
                    JoinedDate = DateTime.UtcNow
                };
                _context.TeamMembers.Add(teamMember);
            }

            await _context.SaveChangesAsync();

            var teamDetails = await _context.Teams
                .Include(t => t.Manager)
                .Include(t => t.TeamMembers)
                    .ThenInclude(tm => tm.Employee)
                .FirstOrDefaultAsync(t => t.TeamId == team.TeamId);

            var teamDTO = new TeamDTO
            {
                TeamId = teamDetails.TeamId,
                TeamName = teamDetails.TeamName,
                Description = teamDetails.Description,
                CreatedDate = teamDetails.CreatedDate,
                ManagerId = teamDetails.ManagerId,
                ManagerName = teamDetails.Manager.Username,
                Members = teamDetails.TeamMembers.Select(tm => new TeamMemberDTO
                {
                    TeamMemberId = tm.TeamMemberId,
                    EmployeeId = tm.EmployeeId,
                    Username = tm.Employee.Username,
                    Email = tm.Employee.Email,
                    JoinedDate = tm.JoinedDate
                }).ToList()
            };

            return CreatedAtAction(nameof(GetTeamById), new { id = team.TeamId }, teamDTO);
        }

        // Get all teams for a manager
        [HttpGet("manager/{managerId}")]
        public async Task<IActionResult> GetManagerTeams(int managerId)
        {
            var teams = await _context.Teams
                .Where(t => t.ManagerId == managerId)
                .Select(t => new TeamDTO
                {
                    TeamId = t.TeamId,
                    TeamName = t.TeamName,
                    Description = t.Description,
                    CreatedDate = t.CreatedDate,
                    ManagerId = t.ManagerId,
                    ManagerName = t.Manager.Username
                })
                .ToListAsync();

            return Ok(teams);
        }

        // Get team by ID with members
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTeamById(int id)
        {
            var team = await _context.Teams
                .Include(t => t.Manager)
                .FirstOrDefaultAsync(t => t.TeamId == id);

            if (team == null)
            {
                return NotFound($"Team with ID {id} not found.");
            }

            var teamMembers = await _context.TeamMembers
                .Include(tm => tm.Employee)
                .Where(tm => tm.TeamId == id)
                .Select(tm => new TeamMemberDTO
                {
                    TeamMemberId = tm.TeamMemberId,
                    EmployeeId = tm.EmployeeId,
                    Username = tm.Employee.Username,
                    Email = tm.Employee.Email,
                    JoinedDate = tm.JoinedDate
                })
                .ToListAsync();

            var teamDTO = new TeamDTO
            {
                TeamId = team.TeamId,
                TeamName = team.TeamName,
                Description = team.Description,
                CreatedDate = team.CreatedDate,
                ManagerId = team.ManagerId,
                ManagerName = team.Manager.Username,
                Members = teamMembers
            };

            return Ok(teamDTO);
        }

        // NEW: Get team members by team ID
        [HttpGet("members/{teamId}")]
        public async Task<IActionResult> GetTeamMembers(int teamId)
        {
            var team = await _context.Teams
                .FirstOrDefaultAsync(t => t.TeamId == teamId);

            if (team == null)
            {
                return NotFound($"Team with ID {teamId} not found.");
            }

            var teamMembers = await _context.TeamMembers
                .Include(tm => tm.Employee)
                .Where(tm => tm.TeamId == teamId)
                .Select(tm => new TeamMemberDTO
                {
                    TeamMemberId = tm.TeamMemberId,
                    EmployeeId = tm.EmployeeId,
                    Username = tm.Employee.Username,
                    Email = tm.Employee.Email,
                    JoinedDate = tm.JoinedDate
                })
                .ToListAsync();

            if (!teamMembers.Any())
            {
                return NotFound($"No members found for team with ID {teamId}.");
            }

            return Ok(teamMembers);
        }








        // Add members to a team
        [HttpPost("addMembers")]
        public async Task<IActionResult> AddTeamMembers([FromBody] AddTeamMembersDTO addTeamMembersDTO)
        {
            var team = await _context.Teams
                .FirstOrDefaultAsync(t => t.TeamId == addTeamMembersDTO.TeamId);

            if (team == null)
            {
                return NotFound($"Team with ID {addTeamMembersDTO.TeamId} not found.");
            }

            var employeeIds = addTeamMembersDTO.EmployeeIds;
            var employees = await _context.Employees
                .Where(e => employeeIds.Contains(e.EmployeeId))
                .ToListAsync();

            if (employees.Count != employeeIds.Count)
            {
                return BadRequest("One or more employees do not exist.");
            }

            var existingMembers = await _context.TeamMembers
                .Where(tm => tm.TeamId == addTeamMembersDTO.TeamId && employeeIds.Contains(tm.EmployeeId))
                .Select(tm => tm.EmployeeId)
                .ToListAsync();

            if (existingMembers.Any())
            {
                return BadRequest($"Employees with IDs {string.Join(", ", existingMembers)} are already team members.");
            }

            foreach (var employeeId in employeeIds)
            {
                var teamMember = new TeamMember
                {
                    TeamId = addTeamMembersDTO.TeamId,
                    EmployeeId = employeeId,
                    JoinedDate = DateTime.UtcNow
                };
                _context.TeamMembers.Add(teamMember);
            }

            await _context.SaveChangesAsync();

            return Ok(new { Message = $"{employeeIds.Count} members added to the team successfully." });
        }

        // Remove a member from a team
        [HttpDelete("removeMember")]
        public async Task<IActionResult> RemoveTeamMember([FromBody] RemoveTeamMemberDTO removeTeamMemberDTO)
        {
            var teamMember = await _context.TeamMembers
                .FirstOrDefaultAsync(tm =>
                    tm.TeamId == removeTeamMemberDTO.TeamId &&
                    tm.EmployeeId == removeTeamMemberDTO.EmployeeId);

            if (teamMember == null)
            {
                return NotFound("Team member not found.");
            }

            _context.TeamMembers.Remove(teamMember);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Team member removed successfully." });
        }

        // Update team details
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTeam(int id, [FromBody] CreateTeamDTO updateTeamDTO)
        {
            var team = await _context.Teams
                .FirstOrDefaultAsync(t => t.TeamId == id);

            if (team == null)
            {
                return NotFound($"Team with ID {id} not found.");
            }

            if (team.ManagerId != updateTeamDTO.ManagerId)
            {
                var manager = await _context.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeId == updateTeamDTO.ManagerId && e.RoleID == 2);

                if (manager == null)
                {
                    return BadRequest($"Manager with ID {updateTeamDTO.ManagerId} not found or doesn't have manager role.");
                }
            }

            team.TeamName = updateTeamDTO.TeamName;
            team.Description = updateTeamDTO.Description;
            team.ManagerId = updateTeamDTO.ManagerId;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Team updated successfully." });
        }

        // Delete a team
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTeam(int id)
        {
            var team = await _context.Teams
                .FirstOrDefaultAsync(t => t.TeamId == id);

            if (team == null)
            {
                return NotFound($"Team with ID {id} not found.");
            }

            _context.Teams.Remove(team);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Team deleted successfully." });
        }
        [HttpGet("employee/{employeeId}")]
        public async Task<IActionResult> GetTeamsByEmployeeId(int employeeId)
        {
            // Verify the employee exists
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

            if (employee == null)
            {
                return NotFound($"Employee with ID {employeeId} not found.");
            }

            // Find teams the employee is a member of
            var teams = await _context.TeamMembers
                .Where(tm => tm.EmployeeId == employeeId)
                .Include(tm => tm.Team)
                    .ThenInclude(t => t.Manager)
                .Include(tm => tm.Team)
                    .ThenInclude(t => t.TeamMembers)
                        .ThenInclude(tm => tm.Employee)
                .Select(tm => tm.Team)
                .Distinct()
                .ToListAsync();

            if (!teams.Any())
            {
                return NotFound($"No teams found for employee with ID {employeeId}.");
            }

            // Map teams to TeamDTO
            var teamDTOs = teams.Select(team => new TeamDTO
            {
                TeamId = team.TeamId,
                TeamName = team.TeamName,
                Description = team.Description,
                CreatedDate = team.CreatedDate,
                ManagerId = team.ManagerId,
                ManagerName = team.Manager?.Username,
                Members = team.TeamMembers
                    .Where(tm => tm.EmployeeId != employeeId) // Exclude the requesting employee
                    .Select(tm => new TeamMemberDTO
                    {
                        TeamMemberId = tm.TeamMemberId,
                        EmployeeId = tm.EmployeeId,
                        Username = tm.Employee.Username,
                        Email = tm.Employee.Email,
                        JoinedDate = tm.JoinedDate
                    })
                    .ToList()
            }).ToList();

            return Ok(teamDTOs);
        }
    }
}