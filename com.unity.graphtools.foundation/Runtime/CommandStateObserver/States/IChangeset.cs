using System;
using System.Collections.Generic;

namespace UnityEngine.GraphToolsFoundation.CommandStateObserver
{
    /// <summary>
    /// Interface for changesets of <see cref="IStateComponent"/>.
    /// </summary>
    public interface IChangeset
    {
        /// <summary>
        /// Clears the changeset.
        /// </summary>
        void Clear();

        /// <summary>
        /// Makes this changeset a changeset that summarize <paramref name="changesets"/>.
        /// </summary>
        /// <param name="changesets">The changesets to summarize.</param>
        void AggregateFrom(IEnumerable<IChangeset> changesets);
    }
}
