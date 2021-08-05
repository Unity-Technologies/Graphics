using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Helper to build and render a mesh for Gizmos, it is a lot more faster than drawing a ton of gizmos separately
    /// </summary>
    class MeshGizmo : IDisposable
    {
        class MeshData
        {
            public Mesh mesh;

            public List<Vector3> vertices;
            public List<int> indices;
            public List<Color> colors;

            public MeshData(int capacity)
            {
                vertices = new List<Vector3>(capacity);
                indices = new List<int>(capacity);
                colors = new List<Color>(capacity);
                mesh = new Mesh { indexFormat = IndexFormat.UInt32, hideFlags = HideFlags.HideAndDontSave };
            }

            public void Clear()
            {
                vertices.Clear();
                indices.Clear();
                colors.Clear();
            }

            public void Draw(Matrix4x4 trs, Material mat, MeshTopology topology, CompareFunction depthTest, string gizmoName)
            {
                if (indices.Count == 0)
                    return;

                mesh.Clear();
                mesh.SetVertices(vertices);
                mesh.SetColors(colors);
                mesh.SetIndices(indices, topology, 0);

                mat.SetFloat("_HandleZTest", (int)depthTest);

                var cmd = CommandBufferPool.Get(gizmoName ?? "Mesh Gizmo Rendering");
                cmd.DrawMesh(mesh, trs, mat, 0, 0);
                Graphics.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            public void Dispose()
            {
                CoreUtils.Destroy(mesh);
            }
        }

        MeshData m_LineMesh;
        MeshData m_TriangleMesh;

        Material wireMaterial;
        Material dottedWireMaterial;
        Material solidMaterial;

        public MeshGizmo(int lineCount = 0, int triangleCount = 0)
        {
            m_LineMesh = new MeshData(lineCount * 2);
            m_TriangleMesh = new MeshData(triangleCount * 3);

#if UNITY_EDITOR
            wireMaterial = (Material)UnityEditor.EditorGUIUtility.LoadRequired("SceneView/HandleLines.mat");
            dottedWireMaterial = (Material)UnityEditor.EditorGUIUtility.LoadRequired("SceneView/HandleDottedLines.mat");
            solidMaterial = UnityEditor.HandleUtility.handleMaterial;
#endif
        }

        public void Clear()
        {
            m_LineMesh.Clear();
            m_TriangleMesh.Clear();
        }

        void AddLine(Vector3 p1, Vector3 p2, Color color)
        {
            m_LineMesh.vertices.Add(p1);
            m_LineMesh.vertices.Add(p2);
            m_LineMesh.indices.Add(m_LineMesh.indices.Count);
            m_LineMesh.indices.Add(m_LineMesh.indices.Count);
            m_LineMesh.colors.Add(color);
            m_LineMesh.colors.Add(color);
        }

        void AddTriangle(Vector3 p1, Vector3 p2, Vector3 p3, Color color)
        {
            int firstIndex = m_TriangleMesh.vertices.Count;
            m_TriangleMesh.vertices.Add(p1);
            m_TriangleMesh.vertices.Add(p2);
            m_TriangleMesh.vertices.Add(p3);
            m_TriangleMesh.indices.Add(firstIndex++);
            m_TriangleMesh.indices.Add(firstIndex++);
            m_TriangleMesh.indices.Add(firstIndex);
            m_TriangleMesh.colors.Add(color);
            m_TriangleMesh.colors.Add(color);
            m_TriangleMesh.colors.Add(color);
        }

        void AddQuad(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, Color color)
        {
            int firstIndex = m_TriangleMesh.vertices.Count;

            m_TriangleMesh.vertices.Add(p1);
            m_TriangleMesh.vertices.Add(p2);
            m_TriangleMesh.vertices.Add(p3);
            m_TriangleMesh.vertices.Add(p4);
            m_TriangleMesh.colors.Add(color);
            m_TriangleMesh.colors.Add(color);
            m_TriangleMesh.colors.Add(color);
            m_TriangleMesh.colors.Add(color);
            m_TriangleMesh.indices.Add(firstIndex);
            m_TriangleMesh.indices.Add(firstIndex + 1);
            m_TriangleMesh.indices.Add(firstIndex + 2);
            m_TriangleMesh.indices.Add(firstIndex);
            m_TriangleMesh.indices.Add(firstIndex + 2);
            m_TriangleMesh.indices.Add(firstIndex + 3);
        }

        public void AddWireCube(Vector3 center, Vector3 size, Color color)
        {
            var halfSize = size / 2.0f;
            Vector3 p0 = new Vector3(halfSize.x, halfSize.y, halfSize.z);
            Vector3 p1 = new Vector3(-halfSize.x, halfSize.y, halfSize.z);
            Vector3 p2 = new Vector3(-halfSize.x, -halfSize.y, halfSize.z);
            Vector3 p3 = new Vector3(halfSize.x, -halfSize.y, halfSize.z);
            Vector3 p4 = new Vector3(halfSize.x, halfSize.y, -halfSize.z);
            Vector3 p5 = new Vector3(-halfSize.x, halfSize.y, -halfSize.z);
            Vector3 p6 = new Vector3(-halfSize.x, -halfSize.y, -halfSize.z);
            Vector3 p7 = new Vector3(halfSize.x, -halfSize.y, -halfSize.z);

            AddLine(center + p0, center + p1, color);
            AddLine(center + p1, center + p2, color);
            AddLine(center + p2, center + p3, color);
            AddLine(center + p3, center + p0, color);

            AddLine(center + p4, center + p5, color);
            AddLine(center + p5, center + p6, color);
            AddLine(center + p6, center + p7, color);
            AddLine(center + p7, center + p4, color);

            AddLine(center + p0, center + p4, color);
            AddLine(center + p1, center + p5, color);
            AddLine(center + p2, center + p6, color);
            AddLine(center + p3, center + p7, color);
        }

        public void AddCube(Vector3 center, Vector3 size, Color color)
        {
            var halfSize = size / 2.0f;
            Vector3 p0 = new Vector3(halfSize.x, halfSize.y, halfSize.z);
            Vector3 p1 = new Vector3(-halfSize.x, halfSize.y, halfSize.z);
            Vector3 p2 = new Vector3(-halfSize.x, -halfSize.y, halfSize.z);
            Vector3 p3 = new Vector3(halfSize.x, -halfSize.y, halfSize.z);
            Vector3 p4 = new Vector3(halfSize.x, halfSize.y, -halfSize.z);
            Vector3 p5 = new Vector3(-halfSize.x, halfSize.y, -halfSize.z);
            Vector3 p6 = new Vector3(-halfSize.x, -halfSize.y, -halfSize.z);
            Vector3 p7 = new Vector3(halfSize.x, -halfSize.y, -halfSize.z);

            AddQuad(center + p0, center + p1, center + p2, center + p3, color);
            AddQuad(center + p4, center + p5, center + p6, center + p7, color);
            AddQuad(center + p0, center + p3, center + p7, center + p4, color);
            AddQuad(center + p1, center + p2, center + p6, center + p5, color);
            AddQuad(center + p0, center + p1, center + p5, center + p4, color);
            AddQuad(center + p2, center + p3, center + p7, center + p6, color);
        }

        public void Draw(Matrix4x4 trs, CompareFunction depthTest = CompareFunction.LessEqual, string gizmoName = null)
        {
            m_LineMesh.Draw(trs, wireMaterial, MeshTopology.Lines, depthTest, gizmoName);
            m_TriangleMesh.Draw(trs, wireMaterial, MeshTopology.Triangles, depthTest, gizmoName);
        }

        public void Dispose()
        {
            m_LineMesh.Dispose();
            m_TriangleMesh.Dispose();
        }
    }
}
