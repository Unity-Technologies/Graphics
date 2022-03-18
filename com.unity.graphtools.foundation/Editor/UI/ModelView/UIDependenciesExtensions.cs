using System;
using System.Collections.Generic;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Extension methods for managing dependencies.
    /// </summary>
    static class UIDependenciesExtensions
    {
        /// <summary>
        /// Gets the UIs that depends on a model. They need to be updated when the model changes.
        /// </summary>
        /// <param name="model">The model for which we're querying the UI.</param>
        public static IEnumerable<IModelView> GetDependencies(this IGraphElementModel model)
        {
            return UIDependencies.GetModelDependencies(model);
        }
    }
}
