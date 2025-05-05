namespace server.Controllers

{

    using Microsoft.AspNetCore.Mvc;

    using server.DTO;

    using server.Services;

    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Cors;
 
    [Route("api/auth")]

    [ApiController]

    [EnableCors("AllowSpecificOrigins")]

    public class AuthController : ControllerBase

    {

        private readonly IAuthService _authService;
 
        public AuthController(IAuthService authService)

        {

            _authService = authService;

        }
 
        [HttpPost("send-otp")]

        public async Task<IActionResult> SendOTP([FromBody] EmailDTO model)

        {

            var result = await _authService.SendOTPAsync(model);
 
            // If email is already registered, return a BadRequest response

            if (result == "Email is already registered. Please use a different email or login.")

            {

                return BadRequest(new { message = result });

            }
 
            return Ok(new { message = result });

        }
 
        [HttpPost("verify-otp")]

        public async Task<IActionResult> VerifyOTP([FromBody] VerifyOTPDTO model)

        {

            // Log the incoming data to help with debugging

            Console.WriteLine($"Received OTP verification request: email:{model.Email} OTP: {model.OTP}");
 
            var result = await _authService.VerifyOTPAsync(model);
 
            // If OTP verification failed, return a BadRequest response

            if (result != "OTP verified successfully!")

            {

                return BadRequest(new { message = result });

            }
 
            return Ok(new { message = result });

        }
 
        [HttpPost("register")]

        public async Task<IActionResult> Register([FromBody] CreatePasswordDTO model)

        {

            var result = await _authService.RegisterAsync(model);
 
            // If registration failed, return a BadRequest response

            if (result != "Account registered successfully!")

            {

                return BadRequest(new { message = result });

            }
 
            return Ok(new { message = result });

        }
 
        [HttpPost("login")]

        public async Task<IActionResult> Login([FromBody] LoginDTO model)

        {

            var loginResult = await _authService.LoginAsync(model);
 
            if (loginResult.Token == "Invalid email or password!")

                return Unauthorized(new { message = loginResult.Token });
 
            return Ok(new

            {

                token = loginResult.Token,

                role = loginResult.Role,

                userId = loginResult.UserId

            });

        }
 
        [HttpPost("forgot-password")]
 
        public async Task<IActionResult> ForgotPassword([FromBody] EmailDTO model)
 
        {
 
            var result = await _authService.ForgotPasswordAsync(model);
 
            // If email is not registered, return BadRequest
 
            if (result == "Email not registered!")
 
            {
 
                return BadRequest(new { message = result });
 
            }
 
            return Ok(new { message = result });
 
        }
 
        [HttpPost("reset-password")]
 
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO model)
 
        {
 
            var result = await _authService.ResetPasswordAsync(model);
 
            // If password reset failed, return a BadRequest response
 
            if (result != "Password reset successfully!")
 
            {
 
                return BadRequest(new { message = result });
 
            }
 
            return Ok(new { message = result });
 
        }
 
    }
 
}
 