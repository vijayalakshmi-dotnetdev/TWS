using System;

namespace TWS.Domain.Models
{
    /// <summary>
    /// Result of authentication operation
    /// Supports multiple property name conventions for compatibility
    /// </summary>
    public class AuthenticationResult
    {
        /// <summary>
        /// Whether authentication was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Alias for Success (for compatibility with different naming conventions)
        /// </summary>
        public bool IsSuccess
        {
            get => Success;
            set => Success = value;
        }

        /// <summary>
        /// Authentication token (can be intermediate token or final access token)
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Alias for Token (for compatibility)
        /// </summary>
        public string AccessToken
        {
            get => Token;
            set => Token = value;
        }

        /// <summary>
        /// Message (success or error description)
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Alias for Message when used for errors (for compatibility)
        /// </summary>
        public string ErrorMessage
        {
            get => Message;
            set => Message = value;
        }

        /// <summary>
        /// Whether OTP is required for next authentication step
        /// </summary>
        public bool RequiresOTP { get; set; }

        /// <summary>
        /// User ID
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// User's display name
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Session ID
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// Token expiry time
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Alias for ExpiresAt (for compatibility)
        /// </summary>
        public DateTime? TokenExpiry
        {
            get => ExpiresAt;
            set => ExpiresAt = value;
        }

        /// <summary>
        /// WebSocket token (if different from main access token)
        /// </summary>
        public string WebSocketToken { get; set; }

        /// <summary>
        /// Creates a new authentication result with default values
        /// </summary>
        public AuthenticationResult()
        {
            Success = false;
            Message = string.Empty;
            RequiresOTP = false;
        }

        /// <summary>
        /// Creates a successful authentication result
        /// </summary>
        public static AuthenticationResult CreateSuccess(string token, string message = "Authentication successful")
        {
            return new AuthenticationResult
            {
                Success = true,
                Token = token,
                Message = message
            };
        }

        /// <summary>
        /// Creates a failed authentication result
        /// </summary>
        public static AuthenticationResult CreateFailure(string message)
        {
            return new AuthenticationResult
            {
                Success = false,
                Message = message
            };
        }

        /// <summary>
        /// Creates a result indicating OTP is required
        /// </summary>
        public static AuthenticationResult CreateOTPRequired(string token, string message = "OTP required")
        {
            return new AuthenticationResult
            {
                Success = true,
                Token = token,
                Message = message,
                RequiresOTP = true
            };
        }

        public override string ToString()
        {
            if (Success)
            {
                var tokenPreview = Token?.Length > 10 ? Token.Substring(0, 10) + "..." : Token;
                return $"Success: {Message}, Token: {tokenPreview}";
            }
            else
            {
                return $"Failed: {Message}";
            }
        }
    }
}