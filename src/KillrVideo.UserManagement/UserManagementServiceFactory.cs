using System.ComponentModel.Composition;
using Cassandra;
using DryIocAttributes;
using Grpc.Core;
using KillrVideo.MessageBus;

namespace KillrVideo.UserManagement
{
    /// <summary>
    /// Static factory for creating a ServerServiceDefinition for the User Management Service for use with a Grpc Server.
    /// </summary>
    [Export, AsFactory]
    public static class UserManagementServiceFactory
    {
        [Export]
        public static ServerServiceDefinition Create(ISession cassandra, IBus bus)
        {
            var userManagement = new UserManagementServiceImpl(cassandra, bus);
            return UserManagementService.BindService(userManagement);
        }
    }
}
