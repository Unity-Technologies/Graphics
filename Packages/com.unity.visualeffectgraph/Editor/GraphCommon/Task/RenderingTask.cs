using UnityEngine;

namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// Task that renders geometry using a specific material.
    /// </summary>
    /*public*/ class RenderingTask : ITask
    {
        /// <summary>
        /// Gets the material used to render the geometry.
        /// </summary>
        public Material Material { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderingTask"/> class.
        /// </summary>
        /// <param name="material">The material used by the task.</param>
        public RenderingTask(Material material)
        {
            Material = material;
        }
    }
}
