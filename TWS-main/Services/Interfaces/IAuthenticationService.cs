using System.Threading.Tasks;
using TWS.Domain.Models;

namespace TWS.Services.Interfaces
{
    /// <summary>
    /// Service for authentication operations
    /// </summary>
    public interface IAuthenticationService
    {
        /// <summary>
        /// Performs login with user ID and password (returns token for OTP step)
        /// </summary>
        Task<AuthenticationResult> LoginAsync(string userId, string password);

        /// <summary>
        /// Requests OTP for user
        /// </summary>
        Task<AuthenticationResult> RequestOTPAsync(string userId);

        /// <summary>
        /// Validates OTP and completes authentication
        /// </summary>
        Task<AuthenticationResult> ValidateOTPAsync(string userId, string otp,string deviceNumber);

        /// <summary>
        /// Gets the current authentication token
        /// </summary>
        string GetAuthToken();

        /// <summary>
        /// Checks if user is authenticated
        /// </summary>
        bool IsAuthenticated();

        /// <summary>
        /// Logs out the current user
        /// </summary>
        void Logout();
        Task<AuthenticationResult> ValidateOTPAsync(string token, string userId, string otp, string deviceNumber);
    }
}
