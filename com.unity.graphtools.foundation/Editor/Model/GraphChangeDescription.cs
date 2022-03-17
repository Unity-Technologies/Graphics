using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Describes changes made to a graph model.
    /// </summary>
    public class GraphChangeDescription
    {
        /// <summary>
        /// The new models.
        /// </summary>
        public IEnumerable<IGraphElementModel> NewModels { get; private set; }

        /// <summary>
        /// The changed models.
        /// </summary>
        public IReadOnlyDictionary<IGraphElementModel, IReadOnlyList<ChangeHint>> ChangedModels { get; private set; }

        /// <summary>
        /// The deleted models.
        /// </summary>
        public IEnumerable<IGraphElementModel> DeletedModels { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphChangeDescription"/> class.
        /// </summary>
        public GraphChangeDescription()
            : this(null, null, null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphChangeDescription"/> class.
        /// </summary>
        /// <param name="newModels">The new models.</param>
        /// <param name="changedModels">The changed models, with hints about what changed.</param>
        /// <param name="deletedModels">The deleted models.</param>
        public GraphChangeDescription(
            IEnumerable<IGraphElementModel> newModels,
            IReadOnlyDictionary<IGraphElementModel, IReadOnlyList<ChangeHint>> changedModels,
            IEnumerable<IGraphElementModel> deletedModels)
        {
            NewModels = newModels ?? Enumerable.Empty<IGraphElementModel>();
            ChangedModels = changedModels ?? new Dictionary<IGraphElementModel, IReadOnlyList<ChangeHint>>();
            DeletedModels = deletedModels ?? Enumerable.Empty<IGraphElementModel>();
        }

        /// <summary>
        /// Merges <paramref name="other"/> into this.
        /// </summary>
        /// <param name="other">The other change description to merge in this change description.</param>
        public void Union(GraphChangeDescription other)
        {
            Union(other.NewModels, other.ChangedModels, other.DeletedModels);
        }

        /// <summary>
        /// Merges change descriptions into this.
        /// </summary>
        /// <param name="newModels">The new models.</param>
        /// <param name="changedModels">The changed models.</param>
        /// <param name="deletedModels">The deleted models.</param>
        public void Union(
            IEnumerable<IGraphElementModel> newModels,
            IReadOnlyDictionary<IGraphElementModel, IReadOnlyList<ChangeHint>> changedModels,
            IEnumerable<IGraphElementModel> deletedModels)
        {
            if (newModels != null)
                NewModels = NewModels.Union(newModels);

            if (deletedModels != null)
                DeletedModels = DeletedModels.Union(deletedModels);

            if (changedModels != null)
            {
                // Convert ChangedModels to a writable dictionary.
                var writableChangedModels = ChangedModels as Dictionary<IGraphElementModel, IReadOnlyList<ChangeHint>>;
                writableChangedModels ??= ChangedModels.ToDictionary(kv => kv.Key, kv => kv.Value);

                // Merge changes from changedModels into writableChangedModels.
                foreach (var changedModel in changedModels)
                {
                    if (writableChangedModels.TryGetValue(changedModel.Key, out var hints))
                    {
                        // If writableChangedModels already contains changedModel, merge the hints.

                        // Convert hints to a writable list.
                        var writableHints = hints as List<ChangeHint> ?? hints.ToList();

                        // Add hints from changedModel to rwHint.
                        foreach (var hint in changedModel.Value)
                        {
                            if (!writableHints.Contains(hint))
                            {
                                writableHints.Add(hint);
                            }
                        }

                        writableChangedModels[changedModel.Key] = writableHints;
                    }
                    else
                    {
                        writableChangedModels[changedModel.Key] = changedModel.Value;
                    }

                    ChangedModels = writableChangedModels;
                }
            }
        }
    }
}
