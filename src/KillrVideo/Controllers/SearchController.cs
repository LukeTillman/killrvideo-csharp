using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using KillrVideo.ActionFilters;
using KillrVideo.ActionResults;
using KillrVideo.Data.Users;
using KillrVideo.Data.Users.Dtos;
using KillrVideo.Data.Videos;
using KillrVideo.Data.Videos.Dtos;
using KillrVideo.Models.Search;
using KillrVideo.Models.Shared;
using KillrVideo.Models.Videos;

namespace KillrVideo.Controllers
{
    public class SearchController : ConventionControllerBase
    {
        private readonly IVideoReadModel _videoReadModel;
        private readonly IUserReadModel _userReadModel;

        public SearchController(IVideoReadModel videoReadModel, IUserReadModel userReadModel)
        {
            if (videoReadModel == null) throw new ArgumentNullException("videoReadModel");
            if (userReadModel == null) throw new ArgumentNullException("userReadModel");
            _videoReadModel = videoReadModel;
            _userReadModel = userReadModel;
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

            // TODO:  Better solution than client-side JOIN?
            var authorIds = new HashSet<Guid>(videos.Videos.Select(v => v.UserId));
            IEnumerable<UserProfile> authors = await _userReadModel.GetUserProfiles(authorIds);

            return JsonSuccess(new SearchResultsViewModel
            {
                Tag = model.Tag,
                Videos = videos.Videos.Join(authors, vp => vp.UserId, a => a.UserId,
                                            (vp, a) => VideoPreviewViewModel.FromDataModel(vp, a, Url))
                               .ToList()
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