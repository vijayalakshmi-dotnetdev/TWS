namespace TWS.Infrastructure.Logging
{
    /// <summary>
    /// Logging levels
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Debug level - detailed information for debugging
        /// </summary>
        Debug = 0,

        /// <summary>
        /// Information level - general informational messages
        /// </summary>
        Information = 1,

        /// <summary>
        /// Warning level - potentially harmful situations
        /// </summary>
        Warning = 2,

        /// <summary>
        /// Error level - error events
        /// </summary>
        Error = 3,

        /// <summary>
        /// Critical level - very severe error events
        /// </summary>
        Critical = 4
    }
}