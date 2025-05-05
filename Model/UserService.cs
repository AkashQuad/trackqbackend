
using server.services;
using server.Services;


namespace server.Model
{
    // Session class for registration process
    public class RegistrationSession
    {
        public required string Email { get; set; }
        public bool IsOtpVerified { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    // Session class for password reset process
    public class PasswordResetSession
    {
        public required string Email { get; set; }
        public bool IsOtpVerified { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public partial class userService
    {
        private readonly IEmailService _emailService;
        private readonly IOTPService _otpService;

        // Session storage for registration
        private readonly Dictionary<string, RegistrationSession> _sessionStorage = new();

        // Session storage for password reset
        private readonly Dictionary<string, PasswordResetSession> _resetSessionStorage = new();

        public userService(IEmailService emailService, IOTPService otpService)
        {
            _emailService = emailService;
            _otpService = otpService;
        }

        // Start registration process with OTP
        public async Task<string> StartRegistration(string email)
        {
            // Generate and send OTP
            await _otpService.GenerateOTPAsync(email);

            // Create a session ID
            string sessionId = Guid.NewGuid().ToString();

            // Store session info
            _sessionStorage[sessionId] = new RegistrationSession
            {
                Email = email,
                IsOtpVerified = false
            };

            return sessionId;
        }

        // Verify OTP for registration
        public bool VerifyOtp(string sessionId, string otp)
        {
            if (!_sessionStorage.TryGetValue(sessionId, out var session))
            {
                return false;
            }

            // Verify OTP
            bool isValid = _otpService.VerifyOTPAsync(session.Email, otp).GetAwaiter().GetResult();

            if (isValid)
            {
                session.IsOtpVerified = true;
                return true;
            }

            return false;
        }

        // Get registration session info
        public RegistrationSession? GetSession(string sessionId)
        {
            if (_sessionStorage.TryGetValue(sessionId, out var session))
            {
                return session;
            }
            return null;
        }

        // Remove session
        public void RemoveSession(string sessionId)
        {
            if (_sessionStorage.ContainsKey(sessionId))
            {
                _sessionStorage.Remove(sessionId);
            }
        }

        // Initiate password reset process
        public async Task<string> InitiatePasswordReset(string email)
        {
            // Generate and send OTP
            await _otpService.GenerateOTPAsync(email);

            // Create a reset session ID
            string resetSessionId = Guid.NewGuid().ToString();

            // Store reset session info
            _resetSessionStorage[resetSessionId] = new PasswordResetSession
            {
                Email = email,
                IsOtpVerified = false
            };

            return resetSessionId;
        }

        // Verify OTP for password reset
        public bool VerifyPasswordResetOtp(string resetSessionId, string otp)
        {
            if (!_resetSessionStorage.TryGetValue(resetSessionId, out var session))
            {
                return false;
            }

            // Verify OTP
            bool isValid = _otpService.VerifyOTPAsync(session.Email, otp).GetAwaiter().GetResult();

            if (isValid)
            {
                session.IsOtpVerified = true;
                return true;
            }

            return false;
        }

        // Get password reset session info
        public PasswordResetSession? GetResetSession(string resetSessionId)
        {
            if (_resetSessionStorage.TryGetValue(resetSessionId, out var session))
            {
                return session;
            }
            return null;
        }

        // Remove reset session
        public void RemoveResetSession(string resetSessionId)
        {
            if (_resetSessionStorage.ContainsKey(resetSessionId))
            {
                _resetSessionStorage.Remove(resetSessionId);
            }
        }
    }
}