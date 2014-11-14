using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using KillrVideo.ActionResults;
using KillrVideo.Models.PlaybackStats;
using KillrVideo.Statistics.Messages.Commands;
using Nimbus;

namespace KillrVideo.Controllers
{
    /// <summary>
    /// Controller for handling playback stats.
    /// </summary>
    public class PlaybackStatsController : ConventionControllerBase
    {
        private readonly IBus _bus;

        public PlaybackStatsController(IBus bus)
        {
            if (bus == null) throw new ArgumentNullException("bus");
            _bus = bus;
        }

        /// <summary>
        /// Records video playback started events.
        /// </summary>
        [HttpPost]
        public async Task<JsonNetResult> Started(StartedViewModel model)
        {
            await _bus.Send(new RecordPlaybackStarted { VideoId = model.VideoId });
            return JsonSuccess();
        }
    }
}