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
    [AddComponentMenu("")]
    [HDRPHelpURL("water-exclude-part-of-the-water-surface")]
    [ExecuteAlways]
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
                if (m_ExclusionRenderer.TryGetComponent<MeshFilter>(out var filter))
                    filter.sharedMesh = targetMesh;
            }
        }

        #region Migration
        enum Version
        {
            Initial,
            RayTracingExclusion,

            Count = RayTracingExclusion
        }

        [SerializeField]
        Version version = Version.Initial;

        void Awake()
        {
            if (version == Version.Count)
                return;

            if (version == Version.Initial)
            {
                if (m_ExclusionRenderer != null)
                    m_ExclusionRenderer.GetComponent<MeshRenderer>().rayTracingMode = UnityEngine.Experimental.Rendering.RayTracingMode.Off;
                version++;
            }
        }
        #endregion
    }
}
