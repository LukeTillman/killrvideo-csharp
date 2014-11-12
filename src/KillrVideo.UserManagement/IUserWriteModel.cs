using System.Threading.Tasks;
using KillrVideo.UserManagement.Api.Commands;
using KillrVideo.UserManagement.Dtos;

namespace KillrVideo.UserManagement
{
    public interface IUserWriteModel
    {
        /// <summary>
        /// Creates a new user.  Returns true if successful or false if a user with the email address specified already exists.
        /// </summary>
        Task<bool> CreateUser(CreateUser user);
    }
}