using System;
using System.ComponentModel.Composition;
using DryIocAttributes;
using Grpc.Core;

namespace KillrVideo.UserManagement
{
    /// <summary>
    /// Static factory for creating a ServerServiceDefinition for the User Management Service for use with a Grpc Server.
    /// </summary>
    [Export, AsFactory]
    public class UserManagementServiceFactory
    {
        private readonly Func<UserManagementServiceImpl> _regular;
        private readonly Func<LinqUserManagementService> _linq;

        public UserManagementServiceFactory(Func<UserManagementServiceImpl> regular, Func<LinqUserManagementService> linq)
        {
            if (regular == null) throw new ArgumentNullException(nameof(regular));
            if (linq == null) throw new ArgumentNullException(nameof(linq));
            _regular = regular;
            _linq = linq;
        }

        [Export]
        public ServerServiceDefinition Create()
        {
            var userManagement = _regular();

            // Uncomment this line to use LINQ user management service instead
            // var userManagement = _linq();

            return UserManagementService.BindService(userManagement);
        }
    }
}
