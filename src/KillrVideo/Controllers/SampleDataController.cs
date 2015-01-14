using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using KillrVideo.ActionFilters;
using KillrVideo.ActionResults;
using KillrVideo.Models.SampleData;
using KillrVideo.SampleData;
using KillrVideo.SampleData.Dtos;

namespace KillrVideo.Controllers
{
    /// <summary>
    /// Controller for manually adding more sample data to the site.
    /// </summary>
    [CheckSampleDataEntryEnabled]
    public class SampleDataController : ConventionControllerBase
    {
        private readonly ISampleDataService _sampleDataService;

        public SampleDataController(ISampleDataService sampleDataService)
        {
            if (sampleDataService == null) throw new ArgumentNullException("sampleDataService");
            _sampleDataService = sampleDataService;
        }

        /// <summary>
        /// Shows the UI for adding sample data manually to the site.
        /// </summary>
        [HttpGet]
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Tells the sample data service to add some sample users to the site.
        /// </summary>
        [HttpPost]
        public async Task<JsonNetResult> AddUsers(AddUsersViewModel model)
        {
            await _sampleDataService.AddSampleUsers(new AddSampleUsers { NumberOfUsers = model.NumberOfUsers });
            return JsonSuccess();
        }

        /// <summary>
        /// Tells the sample data service to add some sample YouTube videos to the site.
        /// </summary>
        [HttpPost]
        public async Task<JsonNetResult> AddYouTubeVideos(AddYouTubeVideosViewModel model)
        {
            await _sampleDataService.AddSampleYouTubeVideos(new AddSampleYouTubeVideos { NumberOfVideos = model.NumberOfVideos });
            return JsonSuccess();
        }

        /// <summary>
        /// Tells the sample data service to add some sample comments to the site.
        /// </summary>
        [HttpPost]
        public async Task<JsonNetResult> AddComments(AddCommentsViewModel model)
        {
            await _sampleDataService.AddSampleComments(new AddSampleComments { NumberOfComments = model.NumberOfComments });
            return JsonSuccess();
        }

        /// <summary>
        /// Tells the sample data service to add some sample ratings to some videos on the site.
        /// </summary>
        public async Task<JsonNetResult> AddVideoRatings(AddVideoRatingsViewModel model)
        {
            await _sampleDataService.AddSampleRatings(new AddSampleRatings { NumberOfRatings = model.NumberOfRatings });
            return JsonSuccess();
        }

        /// <summary>
        /// Tells the sample data service to add some sample views to videos on the site.
        /// </summary>
        public async Task<JsonNetResult> AddVideoViews(AddVideoViewsViewModel model)
        {
            await _sampleDataService.AddSampleVideoViews(new AddSampleVideoViews { NumberOfViews = model.NumberOfViews });
            return JsonSuccess();
        }
    }
}