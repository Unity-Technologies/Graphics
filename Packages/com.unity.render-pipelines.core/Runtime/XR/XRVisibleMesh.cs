using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    // Helper class to render the visible mesh  using custom materials.
    // If possible, the mesh for each view will be combined into one mesh to reduce draw calls.
    internal class XRVisibleMesh
    {
        XRPass m_Pass;
        Mesh m_CombinedMesh;
        int m_CombinedMeshHashCode;

        static readonly ProfilingSampler k_VisibleMeshProfilingSampler = new ProfilingSampler("XR Visible Mesh");

        internal XRVisibleMesh(XRPass xrPass)
        {
            m_Pass = xrPass;
        }

        internal void Dispose()
        {
            if (m_CombinedMesh)
            {
                CoreUtils.Destroy(m_CombinedMesh);
                m_CombinedMesh = null;
            }
        }

        internal bool hasValidVisibleMesh
        {
            get
            {
                if (IsVisibleMeshSupported())
                {
                    if (m_Pass.singlePassEnabled)
                        return m_CombinedMesh != null;
                    else
                        return m_Pass.GetVisibleMesh() != null;
                }

                return false;
            }
        }

        internal void RenderVisibleMeshCustomMaterial(CommandBuffer cmd, float occlusionMeshScale, Material material, MaterialPropertyBlock materialBlock, int shaderPass, bool yFlip = false)
        {
            if (IsVisibleMeshSupported())
            {
                using (new ProfilingScope(cmd, k_VisibleMeshProfilingSampler))
                {
                    Vector3 scale = new Vector3(occlusionMeshScale, yFlip ? occlusionMeshScale : -occlusionMeshScale, 1.0f);
                    Mesh VisMesh = m_Pass.singlePassEnabled ? m_CombinedMesh : m_Pass.GetVisibleMesh(0);
                    cmd.DrawMesh(VisMesh, Matrix4x4.Scale(scale), material, 0, shaderPass, materialBlock);
                }
            }
        }

        internal void UpdateCombinedMesh()
        {
            if (IsVisibleMeshSupported() && m_Pass.singlePassEnabled && TryGetVisibleMeshCombinedHashCode(out var hashCode))
            {
                if (m_CombinedMesh == null || hashCode != m_CombinedMeshHashCode)
                {
                    CreateVisibleMeshCombined();
                    m_CombinedMeshHashCode = hashCode;
                }
            }
            else
            {
                m_CombinedMesh = null;
                m_CombinedMeshHashCode = 0;
            }
        }

        bool IsVisibleMeshSupported()
        {
            return m_Pass.enabled && m_Pass.occlusionMeshScale > 0.0f;
        }

        bool TryGetVisibleMeshCombinedHashCode(out int hashCode)
        {
            hashCode = 17;

            for (int viewId = 0; viewId < m_Pass.viewCount; ++viewId)
            {
                Mesh mesh = m_Pass.GetVisibleMesh(viewId);

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

        // Create a new mesh that contains the visible data from all views
        // This essentially fetches the mesh vertices from XRPass.GetVisibleMesh(viewId=0,1)
        // and combines them into one mesh.
        void CreateVisibleMeshCombined()
        {
            CoreUtils.Destroy(m_CombinedMesh);

            m_CombinedMesh = new Mesh();
            m_CombinedMesh.indexFormat = IndexFormat.UInt16;

            int combinedVertexCount = 0;
            uint combinedIndexCount = 0;

            for (int viewId = 0; viewId < m_Pass.viewCount; ++viewId)
            {
                Mesh mesh = m_Pass.GetVisibleMesh(viewId);

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
                Mesh mesh = m_Pass.GetVisibleMesh(viewId);
                var meshIndices = mesh.GetIndices(0);

                // Encode the viewId into the z channel
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
