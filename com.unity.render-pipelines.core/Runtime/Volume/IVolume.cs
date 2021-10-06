using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// An interface for Volumes
    /// </summary>
    public interface IVolume
    {
        /// <summary>
        /// Specifies whether to apply the Volume to the entire Scene or not.
        /// </summary>
        bool isGlobal { get; set; }

        /// <summary>
        /// The colliders of the volume if <see cref="isGlobal"/> is false
        /// </summary>
        List<Collider> colliders { get; }
    }
}
