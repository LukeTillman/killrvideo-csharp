using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KillrVideo.MessageBus
{
    internal static class HelperExtensions
    {
        /// <summary>
        /// Gets all inner exceptions on the AggregateException that aren't TaskCanceledExceptions.
        /// </summary>
        public static IEnumerable<Exception> IgnoreTaskCanceled(this AggregateException ae)
        {
            return ae.InnerExceptions.Where(e => e.GetType() != typeof(TaskCanceledException));
        }
    }
}
