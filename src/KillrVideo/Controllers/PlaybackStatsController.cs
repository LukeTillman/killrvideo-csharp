using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using KillrVideo.ActionResults;
using KillrVideo.Models.PlaybackStats;
using KillrVideo.Statistics;
using KillrVideo.Statistics.Dtos;

namespace KillrVideo.Controllers
{
    /// <summary>
    /// Controller for handling playback stats.
    /// </summary>
    public class PlaybackStatsController : ConventionControllerBase
    {
        private readonly IStatisticsService _stats;
        
        public PlaybackStatsController(IStatisticsService stats)
        {
            if (stats == null) throw new ArgumentNullException("stats");
            _stats = stats;
        }

        /// <summary>
        /// Records video playback started events.
        /// </summary>
        [HttpPost]
        public async Task<JsonNetResult> Started(StartedViewModel model)
        {
            await _stats.RecordPlaybackStarted(new RecordPlaybackStarted { VideoId = model.VideoId });
            return JsonSuccess();
        }
    }
}