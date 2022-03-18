using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface for toolbar element that can be updated by an observer.
    /// </summary>
    public interface IToolbarElement
    {
        /// <summary>
        /// Update the element to reflect changes made to the model.
        /// </summary>
        /// <remarks>For example, implementation can disable the toolbar element if there is no opened graph.</remarks>
        void Update();
    }
}
