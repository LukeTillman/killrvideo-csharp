using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KillrVideo.SampleData.Components
{
    /// <summary>
    /// Component for useful read queries against sample data that are used in multiple places.
    /// </summary>
    public interface IGetSampleData
    {
        /// <summary>
        /// Gets a random collection of sample user ids.  May include duplicate user ids.  Will return either the count specified or an
        /// empty list.
        /// </summary>
        Task<List<Guid>> GetRandomSampleUserIds(int count);

        /// <summary>
        /// Gets a random collection of video ids for videos on the site.  May include duplicate video ids.  Will return either the count
        /// specified or an empty list.
        /// </summary>
        Task<List<Guid>> GetRandomVideoIds(int count);
    }
}
