using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Experimental.Rendering.Universal.LibTessDotNet;
using UnityEngine.Rendering;
using UnityEngine.U2D;

namespace UnityEngine.Experimental.Rendering.Universal
{

    // Simple Cache for SpriteShape Geometry Data.
    [AddComponentMenu("")]
    internal class LightCachedMeshData : MonoBehaviour
    {

        [SerializeField] [HideInInspector] LightUtility.ParametricLightMeshVertex[] m_VertexArray = null;
        [SerializeField] [HideInInspector] ushort[] m_IndexArray = null;
        bool m_RequiresUpload = true;

        // Has Geometry Data
        internal bool HasGeometryData()
        {
            return m_IndexArray.Length > 0 && m_VertexArray.Length > 0;
        }

        // Has Uploaded.
        internal bool RequiresUpload()
        {
            return m_RequiresUpload && HasGeometryData();
        }

        // Set Geometry Cache.
        internal void SetGeometryCache(LightUtility.ParametricLightMeshVertex[] vertexArray, ushort[] indexArray)
        {
            m_IndexArray = indexArray;
            m_VertexArray = vertexArray;
        }

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
