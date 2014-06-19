using System.Collections.Generic;
using System.Web.Mvc;
using KillrVideo.ActionResults;
using KillrVideo.Models.Shared;

namespace KillrVideo.Controllers
{
    /// <summary>
    /// A base class that adds some methods for returning standard JSON responses.
    /// </summary>
    public abstract class ConventionControllerBase : Controller
    {
        /// <summary>
        /// Returns the standard JSON response model indicating success, along with optionally the model specified as the data.
        /// </summary>
        public JsonNetResult JsonSuccess(object model = null)
        {
            var standardModel = new JsonResultViewModel
            {
                Success = true,
                Data = model
            };

            return JsonNetResult.New(standardModel);
        }

        /// <summary>
        /// Returns the standard JSON response model indicating failure, copying any ModelState errors to the Messages collection.
        /// </summary>
        public JsonNetResult JsonFailure()
        {
            IEnumerable<UiMessageViewModel> messages = ModelState.GetErrorUiMessages();

            var standardModel = new JsonResultViewModel
            {
                Success = false,
                Messages = new List<UiMessageViewModel>(messages)
            };

            return JsonNetResult.New(standardModel);
        }
    }
}