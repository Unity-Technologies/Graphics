using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Extension methods for managing dependencies.
    /// </summary>
    public static class ModelDependencies
    {
        static Dictionary<SerializableGUID, HashSet<IModelUI>> s_ModelDependencies = new Dictionary<SerializableGUID, HashSet<IModelUI>>();

        /// <summary>
        /// Adds a dependency between a model and a UI.
        /// </summary>
        /// <param name="model">The model side of the dependency.</param>
        /// <param name="ui">The UI side of the dependency.</param>
        public static void AddDependency(this IGraphElementModel model, IModelUI ui)
        {
            if (!s_ModelDependencies.TryGetValue(model.Guid, out var uiList))
            {
                uiList = new HashSet<IModelUI>();
                s_ModelDependencies[model.Guid] = uiList;
            }

            uiList.Add(ui);
        }

        /// <summary>
        /// Removes a dependency between a model and a UI.
        /// </summary>
        /// <param name="model">The model side of the dependency.</param>
        /// <param name="ui">The UI side of the dependency.</param>
        public static void RemoveDependency(this IGraphElementModel model, IModelUI ui)
        {
            if (s_ModelDependencies.TryGetValue(model.Guid, out var uiList))
            {
                uiList.Remove(ui);
            }
        }

        /// <summary>
        /// Gets the UIs that depends on a model. They need to be updated when the model changes.
        /// </summary>
        /// <param name="model">The model for which we're querying the UI.</param>
        public static IEnumerable<IModelUI> GetDependencies(this IGraphElementModel model)
        {
            return s_ModelDependencies.TryGetValue(model.Guid, out var uiList) ? uiList : Enumerable.Empty<IModelUI>();
        }
    }
}
