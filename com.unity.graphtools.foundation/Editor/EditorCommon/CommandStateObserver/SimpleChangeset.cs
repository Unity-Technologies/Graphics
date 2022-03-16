using System;
using System.Collections.Generic;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Simple change tracking class.
    /// </summary>
    public class SimpleChangeset<TModel> : IChangeset
    {
        /// <summary>
        /// The changed models.
        /// </summary>
        public HashSet<TModel> ChangedModels { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleChangeset{TModel}" /> class.
        /// </summary>
        public SimpleChangeset()
        {
            ChangedModels = new HashSet<TModel>();
        }

        /// <inheritdoc/>
        public void Clear()
        {
            ChangedModels.Clear();
        }

        /// <inheritdoc/>
        public void AggregateFrom(IEnumerable<IChangeset> changesets)
        {
            Clear();
            foreach (var cs in changesets)
            {
                if (cs is SimpleChangeset<TModel> changeset)
                {
                    ChangedModels.UnionWith(changeset.ChangedModels);
                }
            }
        }
    }
}
