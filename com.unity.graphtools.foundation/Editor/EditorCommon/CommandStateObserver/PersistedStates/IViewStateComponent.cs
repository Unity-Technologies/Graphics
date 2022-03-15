using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface for state components that store information for a view.
    /// </summary>
    /// <remarks>
    /// An IViewStateComponent is state information that is tied to a viewGUID only, that usually refer to a <see cref="IRootView"/>.
    /// Example: the breadcrumb that represent the stack of opened assets in a window.
    /// </remarks>
    public interface IViewStateComponent : IStateComponent
    {
        /// <summary>
        /// The unique ID of the referenced view.
        /// </summary>
        Hash128 ViewGuid { get; set; }
    }
}
