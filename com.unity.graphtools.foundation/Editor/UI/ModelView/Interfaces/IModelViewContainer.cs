using System;
using System.Collections.Generic;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Describe a object, usually a <see cref="IModelView"/>, that contains other <see cref="IModelView"/>.
    /// </summary>
    interface IModelViewContainer
    {
        /// <summary>
        /// Returns the first level of included <see cref="IModelView"/> in this element.
        /// </summary>
        IEnumerable<IModelView> ModelViews { get; }
    }
}
