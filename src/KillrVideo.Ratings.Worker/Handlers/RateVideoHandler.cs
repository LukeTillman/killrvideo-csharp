using System;
using System.Threading.Tasks;
using KillrVideo.Ratings.Messages.Commands;
using Nimbus.Handlers;

namespace KillrVideo.Ratings.Worker.Handlers
{
    /// <summary>
    /// Records a user rating a video.
    /// </summary>
    public class RateVideoHandler : IHandleCommand<RateVideo>
    {
        private readonly IRatingsWriteModel _ratingsWriteModel;

        public RateVideoHandler(IRatingsWriteModel ratingsWriteModel)
        {
            if (ratingsWriteModel == null) throw new ArgumentNullException("ratingsWriteModel");
            _ratingsWriteModel = ratingsWriteModel;
        }

        public Task Handle(RateVideo message)
        {
            return _ratingsWriteModel.RateVideo(message);
        }
    }
}
