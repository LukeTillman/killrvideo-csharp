using System;
using KillrVideo.UserManagement.Api.Commands;
using Rebus;

namespace KillrVideo.UserManagement.Worker.Handlers
{
    /// <summary>
    /// Creates new users.
    /// </summary>
    public class CreateUserHandler : IHandleMessages<CreateUser>
    {
        private readonly IUserWriteModel _userWriteModel;

        public CreateUserHandler(IUserWriteModel userWriteModel)
        {
            if (userWriteModel == null) throw new ArgumentNullException("userWriteModel");
            _userWriteModel = userWriteModel;
        }

        public void Handle(CreateUser message)
        {
            _userWriteModel.CreateUser(message);
        }
    }
}
