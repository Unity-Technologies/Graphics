using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface for the inspector view model.
    /// </summary>
    public interface IInspectorModel : IModel, IHasTitle
    {
        /// <summary>
        /// The list of inspection sections.
        /// </summary>
        IReadOnlyList<IInspectorSectionModel> Sections { get; }

        Vector2 ScrollOffset { get; set; }
    }
}
