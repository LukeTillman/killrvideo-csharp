using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KillrVideo.SampleData.Worker.Components
{
    /// <summary>
    /// Component for useful read queries against sample data that are used in multiple places.
    /// </summary>
    public interface IGetSampleData
    {
        /// <summary>
        /// Gets a random collection of sample user ids.
        /// </summary>
        Task<IEnumerable<Guid>> GetRandomSampleUserIds(int maxCount);
    }
}
