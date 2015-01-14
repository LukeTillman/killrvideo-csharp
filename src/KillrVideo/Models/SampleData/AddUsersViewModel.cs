using System;

namespace KillrVideo.Models.SampleData
{
    /// <summary>
    /// View model for adding sample users to the site.
    /// </summary>
    [Serializable]
    public class AddUsersViewModel
    {
        public int NumberOfUsers { get; set; }
    }
}