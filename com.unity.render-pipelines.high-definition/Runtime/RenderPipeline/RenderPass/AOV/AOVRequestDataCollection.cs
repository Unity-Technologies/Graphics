using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    /// <summary>A collection of frame passes. To build one, <see cref="AOVRequestBuilder"/></summary>
    public class AOVRequestDataCollection : IEnumerable<AOVRequestData>, IDisposable
    {
        // Owned
        private List<AOVRequestData> m_AOVRequestData;

        /// <summary>Build a new collection from requests.</summary>
        /// <param name="aovRequestData">Requests to include in the collection.</param>
        public AOVRequestDataCollection(List<AOVRequestData> aovRequestData)
            // Transfer ownership of the list
            => m_AOVRequestData = aovRequestData;

        /// <summary>Enumerate the frame passes.</summary>
        public IEnumerator<AOVRequestData> GetEnumerator() =>
            (m_AOVRequestData ?? Enumerable.Empty<AOVRequestData>()).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Dispose()
        {
            if (m_AOVRequestData == null) return;

            ListPool<AOVRequestData>.Release(m_AOVRequestData);
            m_AOVRequestData = null;
        }
    }
}
