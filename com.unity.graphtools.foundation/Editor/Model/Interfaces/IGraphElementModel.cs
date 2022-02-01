using System.Collections.Generic;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface for a model that represents an element in a graph.
    /// </summary>
    public interface IGraphElementModel
    {
        /// <summary>
        /// The graph model to which the element belongs.
        /// </summary>
        IGraphModel GraphModel { get; }

        /// <summary>
        /// The unique identifier of the element.
        /// </summary>
        SerializableGUID Guid { get; set; }

        /// <summary>
        /// The asset model to which the element belongs.
        /// </summary>
        IGraphAssetModel AssetModel { get; set; }

        /// <summary>
        /// The container for this graph element.
        /// </summary>
        IGraphElementContainer Container { get; }

        /// <summary>
        /// Assign a newly generated GUID to the model.
        /// </summary>
        void AssignNewGuid();

        /// <summary>
        /// The list of capabilities of the element.
        /// </summary>
        IReadOnlyList<Capabilities> Capabilities { get; }

        /// <summary>
        /// Color for the element.
        /// </summary>
        /// <remarks>
        /// Setting a color should set HasUserColor to true.
        /// </remarks>
        Color Color { get; set; }

        /// <summary>
        /// True if the color was changed.
        /// </summary>
        bool HasUserColor { get; }

        /// <summary>
        /// Reset the color to its original state.
        /// </summary>
        /// <remarks>
        /// Resetting a color should set HasUserColor to false.
        /// </remarks>
        void ResetColor();
    }
}
