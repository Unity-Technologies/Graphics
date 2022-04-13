using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    // Helper class to render occlusion meshes.
    // If possible, the mesh for each view will be combined into one mesh to reduce draw calls.
    internal class XROcclusionMesh
    {
        XRPass m_Pass;
        Mesh m_CombinedMesh;
        Material m_Material;
        int m_CombinedMeshHashCode;

        static readonly ProfilingSampler k_OcclusionMeshProfilingSampler = new ProfilingSampler("XR Occlusion Mesh");

        internal XROcclusionMesh(XRPass xrPass)
        {
            m_Pass = xrPass;
        }

        internal void SetMaterial(Material mat)
        {
            m_Material = mat;
        }

        internal bool hasValidOcclusionMesh
        {
            get
            {
                if (IsOcclusionMeshSupported())
                {
                    if (m_Pass.singlePassEnabled)
                        return m_CombinedMesh != null;
                    else
                        return m_Pass.GetOcclusionMesh() != null;
                }

                return false;
            }
        }

        internal void RenderOcclusionMesh(CommandBuffer cmd)
        {
            if (IsOcclusionMeshSupported())
            {
                using (new ProfilingScope(cmd, k_OcclusionMeshProfilingSampler))
                {
                    if (m_Pass.singlePassEnabled)
                    {
                        if (m_CombinedMesh != null && SystemInfo.supportsRenderTargetArrayIndexFromVertexShader)
                        {
                            m_Pass.StopSinglePass(cmd);

                            cmd.EnableShaderKeyword("XR_OCCLUSION_MESH_COMBINED");
                            cmd.DrawMesh(m_CombinedMesh, Matrix4x4.identity, m_Material);
                            cmd.DisableShaderKeyword("XR_OCCLUSION_MESH_COMBINED");

                            m_Pass.StartSinglePass(cmd);
                        }
                    }
                    else
                    {
                        Mesh mesh = m_Pass.GetOcclusionMesh(0);
                        if (mesh != null)
                        {
                            cmd.DrawMesh(mesh, Matrix4x4.identity, m_Material);
                        }
                    }
                }
            }
        }

        internal void UpdateCombinedMesh()
        {
            if (IsOcclusionMeshSupported() && m_Pass.singlePassEnabled && TryGetOcclusionMeshCombinedHashCode(out var hashCode))
            {
                if (m_CombinedMesh == null || hashCode != m_CombinedMeshHashCode)
                {
                    CreateOcclusionMeshCombined();
                    m_CombinedMeshHashCode = hashCode;
                }
            }
            else
            {
                m_CombinedMesh = null;
                m_CombinedMeshHashCode = 0;
            }
        }

        bool IsOcclusionMeshSupported()
        {
            return m_Pass.enabled && m_Material != null;
        }

        bool TryGetOcclusionMeshCombinedHashCode(out int hashCode)
        {
            hashCode = 17;

            for (int viewId = 0; viewId < m_Pass.viewCount; ++viewId)
            {
                Mesh mesh = m_Pass.GetOcclusionMesh(viewId);

                if (mesh != null)
                {
                    hashCode = hashCode * 23 + mesh.GetHashCode();
                }
                else
                {
                    hashCode = 0;
                    return false;
                }
            }

            return true;
        }

        // Create a new mesh that contains the occlusion data from all views
        void CreateOcclusionMeshCombined()
        {
            CoreUtils.Destroy(m_CombinedMesh);

            m_CombinedMesh = new Mesh();
            m_CombinedMesh.indexFormat = IndexFormat.UInt16;

            int combinedVertexCount = 0;
            uint combinedIndexCount = 0;

            for (int viewId = 0; viewId < m_Pass.viewCount; ++viewId)
            {
                Mesh mesh = m_Pass.GetOcclusionMesh(viewId);

                Debug.Assert(mesh != null);
                Debug.Assert(mesh.subMeshCount == 1);
                Debug.Assert(mesh.indexFormat == IndexFormat.UInt16);

                combinedVertexCount += mesh.vertexCount;
                combinedIndexCount += mesh.GetIndexCount(0);
            }

            Vector3[] vertices = new Vector3[combinedVertexCount];
            ushort[] indices = new ushort[combinedIndexCount];
            int vertexStart = 0;
            int indexStart = 0;

            for (int viewId = 0; viewId < m_Pass.viewCount; ++viewId)
            {
                Mesh mesh = m_Pass.GetOcclusionMesh(viewId);
                var meshIndices = mesh.GetIndices(0);

                // Encore the viewId into the z channel
                {
                    mesh.vertices.CopyTo(vertices, vertexStart);

                    for (int i = 0; i < mesh.vertices.Length; i++)
                        vertices[vertexStart + i].z = viewId;
                }

                // Combine indices into one buffer
                for (int i = 0; i < meshIndices.Length; i++)
                {
                    int newIndex = vertexStart + meshIndices[i];
                    Debug.Assert(meshIndices[i] < ushort.MaxValue);

                    indices[indexStart + i] = (ushort)newIndex;
                }

                vertexStart += mesh.vertexCount;
                indexStart += meshIndices.Length;
            }

            m_CombinedMesh.vertices = vertices;
            m_CombinedMesh.SetIndices(indices, MeshTopology.Triangles, 0);
        }
    }
}
