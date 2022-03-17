using System;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// The base interface for models.
    /// </summary>
    public interface IModel
    {
        /// <summary>
        /// The unique identifier of the element.
        /// </summary>
        SerializableGUID Guid { get; set; }

        /// <summary>
        /// Assign a newly generated GUID to the model.
        /// </summary>
        void AssignNewGuid();
    }
}
