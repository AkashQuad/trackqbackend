using server.DTO;

namespace server.Services
{
    public interface IAuthService
    {
        Task<string> SendOTPAsync(EmailDTO model);
        Task<string> VerifyOTPAsync(VerifyOTPDTO model);
        Task<string> RegisterAsync(CreatePasswordDTO model);
        Task<LoginResultDTO> LoginAsync(LoginDTO model);
        Task<string> ForgotPasswordAsync(EmailDTO model);
        Task<string> ResetPasswordAsync(ResetPasswordDTO model);
    }
}