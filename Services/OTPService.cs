using Microsoft.EntityFrameworkCore;

using server.Data;

using server.Model;

using server.services;

using Microsoft.Extensions.Logging;

namespace server.Services

{

    public class OTPService : IOTPService

    {

        private readonly ApplicationDbContext _context;

        private readonly IEmailService _emailService;

        private readonly Random _random = new Random();

        private readonly ILogger<OTPService> _logger;

        public OTPService(ApplicationDbContext context, IEmailService emailService, ILogger<OTPService> logger)

        {

            _context = context;

            _emailService = emailService;

            _logger = logger;

        }

        public async Task<string> GenerateOTPAsync(string email)

        {

            // First, clean up any expired OTPs for this email

            await CleanupExpiredOTPsAsync(email);

            // Generate 6-digit OTP

            string otp = _random.Next(100000, 999999).ToString();

            // Save OTP to database with expiry time

            var otpEntity = await _context.OTPs.FirstOrDefaultAsync(o => o.Email == email && !o.IsUsed);

            if (otpEntity != null)

            {

                // Update existing OTP

                otpEntity.Code = otp;

                otpEntity.CreatedAt = DateTime.UtcNow;

                otpEntity.ExpiresAt = DateTime.UtcNow.AddMinutes(10);

                otpEntity.IsUsed = false;

            }

            else

            {

                // Create new OTP

                otpEntity = new OTP

                {

                    Email = email,

                    Code = otp,

                    CreatedAt = DateTime.UtcNow,

                    ExpiresAt = DateTime.UtcNow.AddMinutes(10),

                    IsUsed = false

                };

                _context.OTPs.Add(otpEntity);

            }

            await _context.SaveChangesAsync();

            // Send OTP via email

            await _emailService.SendEmail(new Model.MailRequest

            {

                Email = email,

                Subject = "Your OTP Code",

                Emailbody = $"<h1>Your OTP Code</h1><p>Your OTP code is: <strong>{otp}</strong></p><p>This code will expire in 10 minutes.</p>"

            });

            return "OTP sent to your email.";

        }

        public async Task<bool> IsOTPValidAsync(string email, string otp)

        {

            // Clean up expired OTPs before validating

            await CleanupExpiredOTPsAsync();

            var otpEntity = await _context.OTPs

                .FirstOrDefaultAsync(o =>

                    o.Email == email &&

                    o.Code == otp &&

                    !o.IsUsed &&

                    o.ExpiresAt > DateTime.UtcNow);

            return otpEntity != null;

        }

        public async Task<bool> VerifyOTPAsync(string email, string otp)

        {

            // Clean up expired OTPs before verifying

            await CleanupExpiredOTPsAsync();

            var otpEntity = await _context.OTPs

                .FirstOrDefaultAsync(o =>

                    o.Email == email &&

                    o.Code == otp &&

                    !o.IsUsed &&

                    o.ExpiresAt > DateTime.UtcNow);

            if (otpEntity == null)

                return false;

            // Mark OTP as used

            otpEntity.IsUsed = true;

            await _context.SaveChangesAsync();

            return true;

        }

        // Clean up expired OTPs for specific email

        private async Task CleanupExpiredOTPsAsync(string email = null)

        {

            try

            {

                IQueryable<OTP> query = _context.OTPs.Where(o => o.ExpiresAt < DateTime.UtcNow);

                // If email is provided, only clean up OTPs for that email

                if (!string.IsNullOrEmpty(email))

                {

                    query = query.Where(o => o.Email == email);

                }

                var expiredOTPs = await query.ToListAsync();

                if (expiredOTPs.Any())

                {

                    _logger.LogInformation($"Removing {expiredOTPs.Count} expired OTPs from database");

                    _context.OTPs.RemoveRange(expiredOTPs);

                    await _context.SaveChangesAsync();

                }

            }

            catch (Exception ex)

            {

                _logger.LogError($"Error cleaning up expired OTPs: {ex.Message}");

                // Continue execution even if cleanup fails

            }

        }

    }

}
