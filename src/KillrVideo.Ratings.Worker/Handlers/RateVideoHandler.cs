using System;
using KillrVideo.Ratings.Api.Commands;
using Rebus;

namespace KillrVideo.Ratings.Worker.Handlers
{
    /// <summary>
    /// Records a user rating a video.
    /// </summary>
    public class RateVideoHandler : IHandleMessages<RateVideo>
    {
        private readonly IRatingsWriteModel _ratingsWriteModel;

        public RateVideoHandler(IRatingsWriteModel ratingsWriteModel)
        {
            if (ratingsWriteModel == null) throw new ArgumentNullException("ratingsWriteModel");
            _ratingsWriteModel = ratingsWriteModel;
        }

        public void Handle(RateVideo message)
        {
            _ratingsWriteModel.RateVideo(message);
        }
    }
}
