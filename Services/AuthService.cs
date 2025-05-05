using System.IdentityModel.Tokens.Jwt;

using System.Security.Claims;

using System.Text;

using Microsoft.Extensions.Configuration;

using Microsoft.IdentityModel.Tokens;

using server.Data;

using server.Model;

using Microsoft.EntityFrameworkCore;

using BC = BCrypt.Net.BCrypt;

using server.DTO;

using server.services;

namespace server.Services

{

    public class AuthService : IAuthService

    {

        private readonly ApplicationDbContext _context;

        private readonly IConfiguration _configuration;

        private readonly IOTPService _otpService;

        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthService(

            ApplicationDbContext context,

            IConfiguration configuration,

            IOTPService otpService,

            IHttpContextAccessor httpContextAccessor)

        {

            _context = context;

            _configuration = configuration;

            _otpService = otpService;

            _httpContextAccessor = httpContextAccessor;

        }

        // Step 1: Send OTP (Registration begins)

        public async Task<string> SendOTPAsync(EmailDTO model)

        {

            // Check if user already exists with a password

            var existingUser = await _context.Employees.FirstOrDefaultAsync(u => u.Email == model.Email);

            if (existingUser != null && !string.IsNullOrEmpty(existingUser.Password))

                return "Email is already registered. Please use a different email or login.";

            // Store email in session for next steps

            _httpContextAccessor.HttpContext.Session.SetString("RegisterEmail", model.Email);

            // Generate and send OTP

            return await _otpService.GenerateOTPAsync(model.Email);

        }

        // Step 2: Verify OTP

        public async Task<string> VerifyOTPAsync(VerifyOTPDTO model)

        {

            // Use email from the DTO instead of from session

            var email = model.Email;

            // Validate the OTP

            var isValid = await _otpService.IsOTPValidAsync(email, model.OTP);

            if (!isValid)

                return "Invalid or expired OTP!";

            // Store both email and OTP in session for the next step

            _httpContextAccessor.HttpContext.Session.SetString("RegisterEmail", email);

            _httpContextAccessor.HttpContext.Session.SetString("VerifiedOTP", model.OTP);

            return "OTP verified successfully!";

        }


        public async Task<string> RegisterAsync(CreatePasswordDTO model)

        {

            // Verify OTP

            var isValid = await _otpService.VerifyOTPAsync(model.Email, model.OTP);

            if (!isValid)

                return "Invalid or expired OTP!";

            // Check if the email already exists as a complete user

            var existingUser = await _context.Employees.FirstOrDefaultAsync(u => u.Email == model.Email);

            if (existingUser != null && !string.IsNullOrEmpty(existingUser.Password))

                return "User already exists with a password!";

            var defaultRoleName = "User";

            // Fetch role, or create it if not exists

            var role = await _context.Roles.SingleOrDefaultAsync(r => r.RoleName == defaultRoleName);

            if (role == null)

            {

                role = new Role { RoleName = "User" };

                _context.Roles.Add(role);

                await _context.SaveChangesAsync();

            }

            // If user exists but doesn't have a password, update the user

            if (existingUser != null)

            {

                existingUser.Username = model.Username; // Use provided username

                existingUser.Password = BC.HashPassword(model.Password);

                existingUser.RoleID = role.RoleID;

            }

            else

            {

                // Create new user

                var employee = new Employee

                {

                    Username = model.Username, // Use provided username

                    Email = model.Email,

                    Password = BC.HashPassword(model.Password),

                    RoleID = role.RoleID,

                    JoinedDate = DateTime.UtcNow

                };

                _context.Employees.Add(employee);

            }

            await _context.SaveChangesAsync();

            return "Account registered successfully!";

        }

        public async Task<LoginResultDTO> LoginAsync(LoginDTO model)

        {

            var user = await _context.Employees.Include(e => e.Role).FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null || !BC.Verify(model.Password, user.Password))

                return new LoginResultDTO { Token = "Invalid email or password!" };

            // Generate JWT Token

            var tokenHandler = new JwtSecurityTokenHandler();

            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ?? throw new ArgumentNullException("JWT Key is missing"));

            var tokenDescriptor = new SecurityTokenDescriptor

            {

                Subject = new ClaimsIdentity(new[]

                {

                    new Claim(ClaimTypes.NameIdentifier, user.EmployeeId.ToString()),

                    new Claim(ClaimTypes.Email, user.Email),

                    new Claim(ClaimTypes.Role, user.Role?.RoleName ?? "User"),
                    new Claim(ClaimTypes.Name, user.Username)


                }),

                //Expires = DateTime.UtcNow.AddHours(Convert.ToDouble(_configuration["Jwt:ExpiryHours"] ?? "1")),


                Expires = DateTime.UtcNow.AddMinutes(120),


                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)

            };

            var token = tokenHandler.CreateToken(tokenDescriptor);

            var tokenString = tokenHandler.WriteToken(token);

            return new LoginResultDTO

            {

                Token = tokenString

            };

        }


        public async Task<string> ForgotPasswordAsync(EmailDTO model)

        {

            // Check if the email exists

            var user = await _context.Employees.FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null)

                return "Email not registered!";

            // Generate and send OTP

            return await _otpService.GenerateOTPAsync(model.Email);

        }

        public async Task<string> ResetPasswordAsync(ResetPasswordDTO model)

        {

            // Instead of relying solely on session, verify using the provided email and OTP

            // Verify OTP

            var isValid = await _otpService.IsOTPValidAsync(model.Email, model.OTP);

            if (!isValid)

                return "Invalid or expired OTP!";

            // Find the user by email

            var user = await _context.Employees.FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null)

                return "User not found!";

            // Update password

            user.Password = BC.HashPassword(model.NewPassword);

            await _context.SaveChangesAsync();

            // Remove the need for session-based reset

            // _httpContextAccessor.HttpContext.Session.Remove("ResetEmail");

            return "Password reset successfully!";

        }

    }

}
