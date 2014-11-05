using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using KillrVideo.ActionResults;
using KillrVideo.Data.PlaybackStats;
using KillrVideo.Models.PlaybackStats;

namespace KillrVideo.Controllers
{
    /// <summary>
    /// Controller for handling playback stats.
    /// </summary>
    public class PlaybackStatsController : ConventionControllerBase
    {
        private readonly IPlaybackStatsWriteModel _statsWriteModel;

        public PlaybackStatsController(IPlaybackStatsWriteModel statsWriteModel)
        {
            if (statsWriteModel == null) throw new ArgumentNullException("statsWriteModel");
            _statsWriteModel = statsWriteModel;
        }

        /// <summary>
        /// Records video playback started events.
        /// </summary>
        [HttpPost]
        public async Task<JsonNetResult> Started(StartedViewModel model)
        {
            await _statsWriteModel.RecordPlaybackStarted(model.VideoId);
            return JsonSuccess();
        }
    }
}