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
        public static readonly int vertexCountPerCube = 24;

        public Mesh mesh;

        List<Vector3> vertices;
        List<int> indices;
        List<Color> colors;

        Material wireMaterial;
        Material dottedWireMaterial;
        Material solidMaterial;

        public MeshGizmo(int capacity = 0)
        {
            vertices = new List<Vector3>(capacity);
            indices = new List<int>(capacity);
            colors = new List<Color>(capacity);
            mesh = new Mesh { indexFormat = IndexFormat.UInt32, hideFlags = HideFlags.HideAndDontSave };
#if UNITY_EDITOR
            wireMaterial = (Material)UnityEditor.EditorGUIUtility.LoadRequired("SceneView/HandleLines.mat");
            dottedWireMaterial = (Material)UnityEditor.EditorGUIUtility.LoadRequired("SceneView/HandleDottedLines.mat");
            solidMaterial = UnityEditor.HandleUtility.handleMaterial;
#endif
        }

        public void Clear()
        {
            vertices.Clear();
            indices.Clear();
            colors.Clear();
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

            AddEdge(center + p0, center + p1);
            AddEdge(center + p1, center + p2);
            AddEdge(center + p2, center + p3);
            AddEdge(center + p3, center + p0);

            AddEdge(center + p4, center + p5);
            AddEdge(center + p5, center + p6);
            AddEdge(center + p6, center + p7);
            AddEdge(center + p7, center + p4);

            AddEdge(center + p0, center + p4);
            AddEdge(center + p1, center + p5);
            AddEdge(center + p2, center + p6);
            AddEdge(center + p3, center + p7);

            void AddEdge(Vector3 p1, Vector3 p2)
            {
                vertices.Add(p1);
                vertices.Add(p2);
                indices.Add(indices.Count);
                indices.Add(indices.Count);
                colors.Add(color);
                colors.Add(color);
            }
        }

        void DrawMesh(Matrix4x4 trs, Material mat, MeshTopology topology, CompareFunction depthTest, string gizmoName)
        {
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetIndices(indices, topology, 0);

            mat.SetFloat("_HandleZTest", (int)depthTest);

            var cmd = CommandBufferPool.Get(gizmoName ?? "Mesh Gizmo Rendering");
            cmd.DrawMesh(mesh, trs, mat, 0, 0);
            Graphics.ExecuteCommandBuffer(cmd);
        }

        public void RenderWireframe(Matrix4x4 trs, CompareFunction depthTest = CompareFunction.LessEqual, string gizmoName = null)
            => DrawMesh(trs, wireMaterial, MeshTopology.Lines, depthTest, gizmoName);

        public void Dispose()
        {
            CoreUtils.Destroy(mesh);
        }
    }
}
