using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using KillrVideo.ActionFilters;
using KillrVideo.ActionResults;
using KillrVideo.Data.Videos;
using KillrVideo.Data.Videos.Dtos;
using KillrVideo.Models.Search;
using KillrVideo.Models.Videos;

namespace KillrVideo.Controllers
{
    public class SearchController : ConventionControllerBase
    {
        private readonly IVideoReadModel _videoReadModel;

        public SearchController(IVideoReadModel videoReadModel)
        {
            if (videoReadModel == null) throw new ArgumentNullException("videoReadModel");
            _videoReadModel = videoReadModel;
        }

        /// <summary>
        /// Shows the search results view.
        /// </summary>
        [HttpGet]
        public ViewResult Results(ShowSearchResultsViewModel model)
        {
            return View(model);
        }

        /// <summary>
        /// Searches videos and returns any results.
        /// </summary>
        [HttpPost]
        public async Task<JsonNetResult> Videos(SearchVideosViewModel model)
        {
            VideosByTag videos = await _videoReadModel.GetVideosByTag(new GetVideosByTag
            {
                Tag = model.Tag,
                PageSize = model.PageSize,
                FirstVideoOnPageVideoId = model.FirstVideoOnPage == null ? (Guid?) null : model.FirstVideoOnPage.VideoId
            });

            return JsonSuccess(new SearchResultsViewModel
            {
                Tag = model.Tag,
                Videos = videos.Videos.Select(VideoPreviewViewModel.FromDataModel).ToList()
            });
        }

        /// <summary>
        /// Searches tags and returns tags suggestions.
        /// </summary>
        [HttpGet, NoCache]
        public async Task<JsonNetResult> SuggestTags(SuggestTagsViewModel model)
        {
            TagsStartingWith tagsStartingWith = await _videoReadModel.GetTagsStartingWith(new GetTagsStartingWith
            {
                TagStartsWith = model.TagStart,
                PageSize = model.PageSize
            });

            return JsonSuccess(new TagResultsViewModel
            {
                TagStart = tagsStartingWith.TagStartsWith,
                Tags = tagsStartingWith.Tags
            });
        }
	}
}