using UnityEngine;

namespace UnityEngine.Experimental.Rendering.Universal
{

    // Simple Cache for Shape Geometry Data.
    // Note: The cache only gets saved in Editor mode.
    // On Runtime, data is only used on Load. Any further changes to Light2D will involve regeneration.
    [AddComponentMenu("")]
    internal class LightCachedMeshData : MonoBehaviour
    {

        [SerializeField] [HideInInspector] LightUtility.ParametricLightMeshVertex[] m_VertexArray = null;
        [SerializeField] [HideInInspector] ushort[] m_IndexArray = null;
        bool m_RequiresUpload = true;

        internal ushort[] indices => m_IndexArray;
        internal LightUtility.ParametricLightMeshVertex[] vertices => m_VertexArray;
        internal bool allowUpdateOnTests = false; // Only Used during Playmode Tests.
        internal bool requiresUpload { get => m_RequiresUpload; set => m_RequiresUpload = value; }

        // Has Geometry Data
        internal bool HasGeometryData()
        {
            return null != m_IndexArray && null != m_VertexArray && m_IndexArray.Length != 0 && m_VertexArray.Length != 0;
        }

        // Has Uploaded.
        internal bool RequiresUpload()
        {
            return m_RequiresUpload && HasGeometryData();
        }

#if UNITY_EDITOR        
        // Set Geometry Cache.
        internal void SetGeometryCache(LightUtility.ParametricLightMeshVertex[] vertexArray, ushort[] indexArray)
        {
            if (!Application.isPlaying || allowUpdateOnTests)
            {
                m_IndexArray = indexArray;
                m_VertexArray = vertexArray;
            }
        }
#endif
        
        internal Bounds Upload(Mesh mesh)
        {
            if (m_RequiresUpload)
            {
                // Update Geometries.
                mesh.SetVertexBufferParams(m_VertexArray.Length, LightUtility.ParametricLightMeshVertex.VertexLayout);
                mesh.SetVertexBufferData(m_VertexArray, 0, 0, m_VertexArray.Length);
                mesh.SetIndices(m_IndexArray, MeshTopology.Triangles, 0, false);
                m_RequiresUpload = false;
            }

            return mesh.GetSubMesh(0).bounds;

        }

    }

}
