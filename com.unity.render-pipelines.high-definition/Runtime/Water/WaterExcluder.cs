using System;
using Unity.Mathematics;
using System.Collections.Generic;
using UnityEditor;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Water excluder component.
    /// </summary>
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    [AddComponentMenu("")]
    public partial class WaterExcluder : MonoBehaviour
    {
        // Mesh that shall be used to exclude water
        [SerializeField]
        internal Mesh m_InternalMesh = null;
        [SerializeField]
        internal GameObject m_ExclusionRenderer = null;

        /// <summary>
        /// Function that sets the mesh used to exclude water surfaces from the final frame.
        /// </summary>
        /// <param name="targetMesh">Defines the mesh that will be used to operate the water exclusion.</param>
        public void SetExclusionMesh(Mesh targetMesh)
        {
            m_InternalMesh = targetMesh;
            if (m_ExclusionRenderer != null)
            {
                MeshFilter filter = null;
                m_ExclusionRenderer.TryGetComponent<MeshFilter>(out filter);
                if (filter != null)
                    filter.sharedMesh = targetMesh;
            }
        }

        void OnDrawGizmos()
        {
            if (m_InternalMesh != null)
            {
                Gizmos.DrawWireMesh(m_InternalMesh, 0, transform.position, transform.rotation, transform.lossyScale);
            }
        }
    }
}
