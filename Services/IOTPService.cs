using server.Model;
using System.Threading.Tasks;

namespace server.Services
{
    public interface IOTPService
    {
        Task<string> GenerateOTPAsync(string email);
        Task<bool> VerifyOTPAsync(string email, string otp);
        Task<bool> IsOTPValidAsync(string email, string otp);
    }
}