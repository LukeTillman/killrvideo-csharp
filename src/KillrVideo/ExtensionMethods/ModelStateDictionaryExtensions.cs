using System.Collections.Generic;
using System.Linq;
using KillrVideo.Models.Shared;

namespace System.Web.Mvc
{
    /// <summary>
    /// Extensions to System.Web.Mvc.ModelStateDictionary
    /// </summary>
    public static class ModelStateDictionaryExtensions
    {
        /// <summary>
        /// Adds multiple error messages with the empty string as the key to the ModelStateDictionary.
        /// </summary>
        public static void AddModelErrors(this ModelStateDictionary modelState, IEnumerable<string> errorMessages)
        {
            foreach (var errorMessage in errorMessages ?? Enumerable.Empty<string>())
                modelState.AddModelError(string.Empty, errorMessage);
        }

        private static IEnumerable<string> GetErrorMessages(this ModelStateDictionary modelStateDictionary)
        {
            if (modelStateDictionary == null)
            {
                return Enumerable.Empty<string>();
            }

            var aggregateErrors = new List<string>();
            foreach (var key in modelStateDictionary.Keys)
            {
                aggregateErrors.AddRange(modelStateDictionary[key].Errors.Where(error => string.IsNullOrWhiteSpace(error.ErrorMessage) == false)
                                                                  .Select(error => error.ErrorMessage));
            }

            return aggregateErrors;
        }

        public static IEnumerable<UiMessageViewModel> GetErrorUiMessages(this ModelStateDictionary modelStateDictionary)
        {
            return modelStateDictionary == null
                       ? Enumerable.Empty<UiMessageViewModel>()
                       : modelStateDictionary.GetErrorMessages().Select(message => new UiMessageViewModel(UiMessageViewModel.MessageType.Error, message));
        }
    }
}