namespace KillrVideo.UserManagement
{
    /// <summary>
    /// Configuration needed by the user management service implementations.
    /// </summary>
    public class UserManagementOptions
    {
        /// <summary>
        /// Whether or not the LINQ implementation is enabled.
        /// </summary>
        public bool LinqEnabled { get; set; }
    }
}
