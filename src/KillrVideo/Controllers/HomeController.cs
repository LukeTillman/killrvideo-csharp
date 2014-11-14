using System;
using System.Web.Mvc;
using KillrVideo.Authentication;
using KillrVideo.Models.Home;
using KillrVideo.Models.Shared;
using KillrVideo.UserManagement;
using KillrVideo.UserManagement.Dtos;
using KillrVideo.Utils;

namespace KillrVideo.Controllers
{
    public class HomeController : Controller
    {
        private readonly IUserReadModel _userReadModel;

        public HomeController(IUserReadModel userReadModel)
        {
            if (userReadModel == null) throw new ArgumentNullException("userReadModel");
            _userReadModel = userReadModel;
        }

        /// <summary>
        /// Shows the home page.
        /// </summary>
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Renders the header used by the shared Razor layout.
        /// </summary>
        [ChildActionOnly]
        public ActionResult Header()
        {
            var model = new ViewNavbarViewModel();

            // If there is a user logged in, lookup their profile
            Guid? userId = User.GetCurrentUserId();
            if (userId != null)
            {
                // Since MVC currently doesn't support async child actions (until ASP.NET vNext), we've got to invoke the async 
                // method synchronously (luckily, we won't deadlock here because our async method is using ConfigureAwait(false)
                // under the covers).  See http://aspnetwebstack.codeplex.com/workitem/601 for details.
                UserProfile profile = _userReadModel.GetUserProfile(userId.Value).Result;
                
                model.LoggedInUser = new UserProfileViewModel
                {
                    UserId = profile.UserId,
                    FirstName = profile.FirstName,
                    LastName = profile.LastName,
                    EmailAddress = profile.EmailAddress,
                    GravatarHash = GravatarHasher.GetHashForEmailAddress(profile.EmailAddress)
                };
            }
            
            return View(model);
        }
    }
}