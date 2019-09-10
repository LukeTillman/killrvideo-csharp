using System;
using System.Collections.Generic;
using System.Linq;

namespace KillrVideo.Host
{
    public static class HelperExtensions
    {
        /// <summary>
        /// Gets all inner exceptions on the AggregateException that aren't TaskCanceledExceptions.
        /// </summary>
        public static IEnumerable<Exception> IgnoreTaskCanceled(this AggregateException ae)
        {
            return ae.InnerExceptions.Where(e => !(e is OperationCanceledException));
        }
    }
}
