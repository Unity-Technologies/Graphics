using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Interface to add additional gizmo renders for a <see cref="IVolume"/>
    /// </summary>
    public interface IVolumeAdditionalGizmo
    {
        /// <summary>
        /// The type that overrides this additional gizmo
        /// </summary>
        Type type { get; }

        /// <summary>
        /// Additional gizmo draw for <see cref="BoxCollider"/>
        /// </summary>
        /// <param name="scr">The <see cref="IVolume"/></param>
        /// <param name="c">The <see cref="BoxCollider"/></param>
        void OnBoxColliderDraw(IVolume scr, BoxCollider c);

        /// <summary>
        /// Additional gizmo draw for <see cref="SphereCollider"/>
        /// </summary>
        /// <param name="scr">The <see cref="IVolume"/></param>
        /// <param name="c">The <see cref="SphereCollider"/></param>
        void OnSphereColliderDraw(IVolume scr, SphereCollider c);

        /// <summary>
        /// Additional gizmo draw for <see cref="MeshCollider"/>
        /// </summary>
        /// <param name="scr">The <see cref="IVolume"/></param>
        /// <param name="c">The <see cref="MeshCollider"/></param>
        void OnMeshColliderDraw(IVolume scr, MeshCollider c);
    }
}
