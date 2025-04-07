using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Defines the basic structure for a Volume, providing the necessary properties for determining
    /// whether the volume should be applied globally to the scene or to specific colliders.
    /// </summary>
    /// <remarks>
    /// This interface serves as a contract for systems that implement volume logic, enabling
    /// reusable code for volume-based behaviors such as rendering effects, post-processing, or scene-specific logic.
    /// The <see cref="IVolume"/> interface is commonly implemented by components that define volumes in a scene,
    /// allowing for flexibility in determining how the volume interacts with the scene. A volume can either be global
    /// (affecting the entire scene) or local (restricted to specific colliders).
    /// This interface is also helpful for drawing gizmos in the scene view, as it allows for visual representation
    /// of volumes in the editor based on their settings.
    /// </remarks>
    public interface IVolume
    {
        /// <summary>
        /// Gets or sets a value indicating whether the volume applies to the entire scene.
        /// If true, the volume is global and affects all objects within the scene.
        /// If false, the volume is local and only affects the objects within the specified colliders.
        /// </summary>
        /// <remarks>
        /// When set to true, the volume's effects will be applied universally across the scene,
        /// without considering individual colliders. When false, the volume will interact only with
        /// the objects inside the colliders defined in <see cref="colliders"/>.
        /// </remarks>
        bool isGlobal { get; set; }

        /// <summary>
        /// A list of colliders that define the area of influence of the volume when <see cref="isGlobal"/> is set to false.
        /// </summary>
        /// <remarks>
        /// This property holds the colliders that restrict the volume's effects to specific areas of the scene.
        /// It is only relevant when <see cref="isGlobal"/> is false, and defines the boundaries of where the volume is applied.
        /// </remarks>
        List<Collider> colliders { get; }
    }

}
