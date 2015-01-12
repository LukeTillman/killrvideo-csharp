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
using KillrVideo.Utils;

namespace KillrVideo.Controllers
{
    public class AccountController : ConventionControllerBase
    {
        private readonly IUserManagementService _userManagement;
        
        public AccountController(IUserManagementService userManagement)
        {
            if (userManagement == null) throw new ArgumentNullException("userManagement");
            _userManagement = userManagement;
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

            // Generate a user Id and try to register the user account (map from view model first)
            Guid userId = Guid.NewGuid();
            var createUser = new CreateUser
            {
                EmailAddress = model.EmailAddress,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Password = model.Password,
                UserId = userId
            };

            // TODO: Validation to try and minimize chance of duplicate users
            // if (await _userWriteModel.CreateUser(createUser) == false)
            // {
            //    ModelState.AddModelError(string.Empty, "A user with that email address already exists.");
            //    return JsonFailure();
            //}

            await _userManagement.CreateUser(createUser);

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

            // Verify the user's credentials
            Guid? userId = await _userManagement.VerifyCredentials(model.EmailAddress, model.Password);
            if (userId == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email address or password");
                return JsonFailure();
            }

            SignTheUserIn(userId.Value);
            return JsonSuccess(new UserSignedInViewModel { AfterLoginUrl = Url.Action("Index", "Home") });
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

            UserProfile profile = await _userManagement.GetUserProfile(userId.Value);
            return View(new AccountInfoViewModel
            {
                UserProfile = UserProfileViewModel.FromDataModel(profile),
                IsCurrentlyLoggedInUser = isCurrentlyLoggedInUser
            });
        }
	}
}