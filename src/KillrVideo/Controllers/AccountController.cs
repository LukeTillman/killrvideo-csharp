using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Security;
using KillrVideo.ActionResults;
using KillrVideo.Authentication;
using KillrVideo.Models.Account;
using KillrVideo.Models.Shared;
using KillrVideo.UserManagement;
using KillrVideo.UserManagement.Dtos;
using KillrVideo.UserManagement.Messages.Commands;
using KillrVideo.Utils;
using Nimbus;

namespace KillrVideo.Controllers
{
    public class AccountController : ConventionControllerBase
    {
        private readonly IUserReadModel _userReadModel;
        private readonly IBus _bus;

        public AccountController(IUserReadModel userReadModel, IBus bus)
        {
            if (userReadModel == null) throw new ArgumentNullException("userReadModel");
            if (bus == null) throw new ArgumentNullException("bus");
            _userReadModel = userReadModel;
            _bus = bus;
        }

        /// <summary>
        /// Shows the account registration view.
        /// </summary>
        [HttpGet]
        public ActionResult Register()
        {
            return View();
        }

        /// <summary>
        /// Registers a new user with the system.
        /// </summary>
        [HttpPost]
        public async Task<JsonNetResult> RegisterUser(RegisterUserViewModel model)
        {
            if (ModelState.IsValid == false)
                return JsonFailure();

            // TODO: Password hashing as part of the user management service

            // Generate a user Id and try to register the user account (map from view model first)
            Guid userId = Guid.NewGuid();
            var createUser = new CreateUser
            {
                EmailAddress = model.EmailAddress,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Password = PasswordHash.CreateHash(model.Password),
                UserId = userId
            };

            // TODO: Validation to try and minimize chance of duplicate users
            // if (await _userWriteModel.CreateUser(createUser) == false)
            // {
            //    ModelState.AddModelError(string.Empty, "A user with that email address already exists.");
            //    return JsonFailure();
            //}

            await _bus.Send(createUser);

            // Assume creation successful so sign the user in
            SignTheUserIn(userId);

            // Return success
            return JsonSuccess(new UserRegisteredViewModel {UserId = userId});
        }

        /// <summary>
        /// Shows the sign-in form.
        /// </summary>
        [HttpGet]
        public ActionResult SignIn()
        {
            return View();
        }

        /// <summary>
        /// Signs a user in.
        /// </summary>
        [HttpPost]
        public async Task<JsonNetResult> SignInUser(SignInUserViewModel model)
        {
            if (ModelState.IsValid == false)
                return JsonFailure();

            // Lookup the user's credentials by email address
            UserCredentials credentials = await _userReadModel.GetCredentials(model.EmailAddress);
            
            // Validate the credentials
            if (credentials == null || PasswordHash.ValidatePassword(model.Password, credentials.Password) == false)
            {
                ModelState.AddModelError(string.Empty, "Invalid email address or password");
                return JsonFailure();
            }

            SignTheUserIn(credentials.UserId);
            
            return JsonSuccess(new UserSignedInViewModel {AfterLoginUrl = Url.Action("Index", "Home")});
        }

        /// <summary>
        /// Signs a user out.
        /// </summary>
        [HttpGet]
        public ActionResult SignOut()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Signs a user in by setting any appropriate cookies.
        /// </summary>
        private void SignTheUserIn(Guid userId)
        {
            // Sign the user in using a Forms auth cookie
            FormsAuthentication.SetAuthCookie(userId.ToString(), false);
        }

        /// <summary>
        /// Shows the account info for a user.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult> Info(GetAccountInfoViewModel model)
        {
            // Whether we're using the currently logged in user
            bool isCurrentlyLoggedInUser = model.UserId.HasValue == false;

            // Use the user id specified, otherwise default to the currently logged in user
            Guid? userId = model.UserId ?? User.GetCurrentUserId();

            if (userId.HasValue == false)
                throw new InvalidOperationException("No user logged in and no user was specified.");

            UserProfile profile = await _userReadModel.GetUserProfile(userId.Value);
            return View(new AccountInfoViewModel
            {
                UserProfile = new UserProfileViewModel
                {
                    UserId = profile.UserId,
                    EmailAddress = profile.EmailAddress,
                    FirstName = profile.FirstName,
                    LastName = profile.LastName,
                    GravatarHash = GravatarHasher.GetHashForEmailAddress(profile.EmailAddress)
                },
                IsCurrentlyLoggedInUser = isCurrentlyLoggedInUser
            });
        }
	}
}