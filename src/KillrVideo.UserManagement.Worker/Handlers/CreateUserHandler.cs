using System;
using System.Threading.Tasks;
using KillrVideo.UserManagement.Messages.Commands;
using Nimbus.Handlers;

namespace KillrVideo.UserManagement.Worker.Handlers
{
    /// <summary>
    /// Creates new users.
    /// </summary>
    public class CreateUserHandler : IHandleCommand<CreateUser>
    {
        private readonly IUserWriteModel _userWriteModel;

        public CreateUserHandler(IUserWriteModel userWriteModel)
        {
            if (userWriteModel == null) throw new ArgumentNullException("userWriteModel");
            _userWriteModel = userWriteModel;
        }

        public Task Handle(CreateUser message)
        {
            return _userWriteModel.CreateUser(message);
        }
    }
}
