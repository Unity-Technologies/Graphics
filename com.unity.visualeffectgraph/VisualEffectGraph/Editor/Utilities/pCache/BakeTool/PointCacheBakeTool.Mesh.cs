using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Utilities
{
    public partial class PointCacheBakeTool : EditorWindow
    {
        public enum MeshBakeMode
        {
            Vertex,
            Edge,
            Triangle
        }

        public enum Distribution
        {
            Sequential,
            Random,
            RandomPerUnit
        }

        Distribution m_Distribution = Distribution.Random;
        MeshBakeMode m_MeshBakeMode = MeshBakeMode.Triangle;
        PCache.Format m_OutputFormat = PCache.Format.Ascii;

        bool m_ExportUV = false;
        bool m_ExportNormals = true;
        bool m_ExportColors = false;
        bool m_UniformPrepass = true;

        EasyMesh m_EasyMesh;
        Mesh m_Mesh;
        int m_OutputPointCount = 4096;
        float m_OutputPointsPerUnit = 15.0f;

        void OnGUI_Mesh()
        {
            GUILayout.Label("Mesh Baking", EditorStyles.boldLabel);
            m_Mesh = (Mesh)EditorGUILayout.ObjectField(Contents.mesh, m_Mesh, typeof(Mesh), false);

            if (m_Mesh != null && (m_EasyMesh == null || m_EasyMesh.mesh != m_Mesh))
            {
                m_EasyMesh = null;
            }

            if (m_EasyMesh == null)
            {
                if (GUILayout.Button("Get Statistics for Mesh"))
                {
                    bool bake = false;
                    if (m_Mesh.vertexCount < 2048)
                        bake = true;
                    else
                    {
                        if (EditorUtility.DisplayDialog("EasyMesh", "This mesh contains a large amount of vertices and parsing geometry can take a long time, are you sure you want to continue?", "Yes", "No"))
                        {
                            bake = true;
                        }
                    }
                    if (bake)
                    {
                        BakeMesh();
                    }
                }
                return;
            }
            m_MeshBakeMode = (MeshBakeMode)EditorGUILayout.EnumPopup(Contents.meshBakeMode, m_MeshBakeMode);
            m_Distribution = (Distribution)EditorGUILayout.EnumPopup(Contents.distribution, m_Distribution);

            m_ExportNormals = EditorGUILayout.Toggle("Export Normals", m_ExportNormals);
            m_ExportColors = EditorGUILayout.Toggle("Export Colors", m_ExportColors);
            m_ExportUV = EditorGUILayout.Toggle("Export UVs", m_ExportUV);

            if (m_Distribution != Distribution.RandomPerUnit)
                m_OutputPointCount = EditorGUILayout.IntField("Point Count", m_OutputPointCount);
            else
                m_OutputPointsPerUnit = EditorGUILayout.FloatField("Points per unit", m_OutputPointsPerUnit);

            m_OutputFormat = (PCache.Format)EditorGUILayout.EnumPopup("File Format", m_OutputFormat);

            if (GUILayout.Button("Save to pCache file..."))
            {
                string fileName = EditorUtility.SaveFilePanelInProject("pCacheFile", m_Mesh.name, "pcache", "Save PCache");
                if (fileName != null)
                {
                    PCache file = new PCache();
                    file.AddVector3Property("position");
                    if (m_ExportNormals) file.AddVector3Property("normal");
                    if (m_ExportColors) file.AddColorProperty("color");
                    if (m_ExportUV) file.AddVector2Property("uv");

                    List<Vector3> positions = new List<Vector3>();
                    List<Vector3> normals = null;
                    List<Vector4> colors = null;
                    List<Vector2> uvs = null;
                    if (m_ExportNormals) normals = new List<Vector3>();
                    if (m_ExportColors) colors = new List<Vector4>();
                    if (m_ExportUV) uvs = new List<Vector2>();
                    try
                    {
                        EditorUtility.DisplayCancelableProgressBar("pCache bake tool", "Capturing data...", 0.0f);
                        GetData(m_EasyMesh, m_OutputPointCount, m_Distribution, m_MeshBakeMode, positions, normals, colors, uvs);

                        EditorUtility.DisplayCancelableProgressBar("pCache bake tool", "Storing data arrays...", 0.5f);
                        file.SetVector3Data("position", positions);
                        if (m_ExportNormals) file.SetVector3Data("normal", normals);
                        if (m_ExportColors) file.SetColorData("color", colors);
                        if (m_ExportUV) file.SetVector2Data("uv", uvs);
                        EditorUtility.DisplayCancelableProgressBar("pCache bake tool", "Saving pCache file", 1.0f);
                        file.SaveToFile(fileName, m_OutputFormat);
                        EditorUtility.ClearProgressBar();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogException(e);
                        EditorUtility.ClearProgressBar();
                    }
                }
            }

            EditorGUILayout.Space();
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Mesh Statistics", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.IntField("Points", m_EasyMesh.m_Points.Count);
                EditorGUILayout.IntField("Vertices", m_EasyMesh.m_Vertices.Count);
                EditorGUILayout.IntField("Edges", m_EasyMesh.m_Edges.Count);
                EditorGUILayout.IntField("Triangles", m_EasyMesh.m_Primitives.Count);
                EditorGUILayout.IntField("Meshes", m_EasyMesh.m_NumMeshes);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
        }

        float[] m_EdgeLength;
        float[] m_TriangleArea;

        void BakeMesh()
        {
            m_EasyMesh = new EasyMesh(m_Mesh, null);
            EditorUtility.DisplayCancelableProgressBar("pCache bake tool", "Gathering Edge Statistics", 0.5f);

            // Pre-bake edge statistics
            int edgeCount = m_EasyMesh.m_Edges.Count;
            m_EdgeLength = new float[edgeCount];

            int i = 0;
            foreach (var edge in m_EasyMesh.m_Edges)
            {
                m_EdgeLength[i] = edge.Length();
                i++;
            }

            EditorUtility.DisplayCancelableProgressBar("pCache bake tool", "Gathering Triangle Statistics", 1.0f);

            // Pre-bake triangle statistics
            int triangleCount = m_EasyMesh.m_Primitives.Count;
            m_TriangleArea = new float[triangleCount];

            i = 0;
            foreach (var triangle in m_EasyMesh.m_Primitives)
            {
                m_TriangleArea[i] = triangle.Area();
                i++;
            }

            EditorUtility.ClearProgressBar();
        }

        void GetVertexData(EasyMesh.Vertex v, List<Vector3> positions, List<Vector3> normals = null, List<Vector4> colors = null, List<Vector2> uvs = null)
        {
            positions.Add(v.Position);

            if (normals != null)
                normals.Add(v.Normal);

            if (colors != null)
                colors.Add(v.Color);

            if (uvs != null)
                uvs.Add(v.UV);
        }

        void GetEdgeData(EasyMesh.Edge e, float position, List<Vector3> positions, List<Vector3> normals = null, List<Vector4> colors = null, List<Vector2> uvs = null)
        {
            positions.Add(Vector3.Lerp(e.A.Position, e.B.Position, position));

            if (normals != null)
                normals.Add(Vector3.Lerp(e.A.Normal, e.B.Normal, position).normalized);

            if (colors != null)
                colors.Add(Color.Lerp(e.A.Color, e.B.Color, position));

            if (uvs != null)
                uvs.Add(Vector2.Lerp(e.A.UV, e.B.UV, position));
        }

        // Barycentric interpolation
        Vector4 BLerp(Vector4 A, Vector4 B, Vector4 C, Vector2 position)
        {
            float x = position.x;
            float y = position.y;
            float sqrtx = Mathf.Sqrt(x);
            return (1 - sqrtx) * A + sqrtx * (1 - y) * B + sqrtx * y * C;
        }

        void GetFaceData(EasyMesh.Primitive p, Vector2 position, List<Vector3> positions, List<Vector3> normals = null, List<Vector4> colors = null, List<Vector2> uvs = null)
        {
            positions.Add(BLerp(p.A.Position, p.B.Position, p.C.Position, position));

            if (normals != null)
                normals.Add(BLerp(p.A.Normal, p.B.Normal, p.C.Normal, position).normalized);

            if (colors != null)
                colors.Add(BLerp(p.A.Color, p.B.Color, p.C.Color, position));

            if (uvs != null)
                uvs.Add(BLerp(p.A.UV, p.B.UV, p.C.UV, position));
        }

        void GetData(EasyMesh m, int count, Distribution distribution, MeshBakeMode bakeMode, List<Vector3> positions, List<Vector3> normals = null, List<Vector4> colors = null, List<Vector2> uvs = null)
        {
            int total = count;

            if (distribution != Distribution.Random)
            {
                switch (bakeMode)
                {
                    case MeshBakeMode.Vertex: total = m.m_Vertices.Count; break;
                    case MeshBakeMode.Edge: total = m.m_Edges.Count; break;
                    case MeshBakeMode.Triangle: total = m.m_Primitives.Count; break;
                }

                if (total < count || distribution == Distribution.RandomPerUnit)
                    count = total;
            }

            for (int i = 0; i < count; i++)
            {
                int index = 0;
                switch (distribution)
                {
                    case Distribution.Sequential: index = i; break;
                    case Distribution.Random:
                        index = Random.Range(0, count);
                        break;
                    case Distribution.RandomPerUnit:
                        index = i;
                        break;
                }
                if (distribution != Distribution.RandomPerUnit)
                {
                    switch (bakeMode)
                    {
                        case MeshBakeMode.Vertex: GetVertexData(m.m_Vertices[index % m.m_Vertices.Count], positions, normals, colors, uvs); break;
                        case MeshBakeMode.Edge: GetEdgeData(m.m_Edges[index % m.m_Edges.Count], Random.Range(0.0f, 1.0f), positions, normals, colors, uvs); break;
                        case MeshBakeMode.Triangle: GetFaceData(m.m_Primitives[index % m.m_Primitives.Count], new Vector2(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f)), positions, normals, colors, uvs); break;
                    }
                }
                else
                {
                    switch (bakeMode)
                    {
                        case MeshBakeMode.Vertex: GetVertexData(m.m_Vertices[index % m.m_Vertices.Count], positions, normals, colors, uvs); break;
                        case MeshBakeMode.Edge:
                            float l = m_EdgeLength[i] * m_OutputPointsPerUnit;
                            int ctl = (int)Mathf.Floor(l) + (Random.Range(0.0f, 1.0f) > (l % 1.0f) ? 1 : 0);

                            for (int ll = 0; ll < ctl; ll++)
                                GetEdgeData(m.m_Edges[index % m.m_Edges.Count], Random.Range(0.0f, 1.0f), positions, normals, colors, uvs);
                            break;

                        case MeshBakeMode.Triangle:
                            float a = Mathf.Sqrt(m_TriangleArea[i]) * m_OutputPointsPerUnit;
                            int cta = (int)Mathf.Floor(a) + (Random.Range(0.0f, 1.0f) > (a % 1.0f) ? 1 : 0);

                            for (int ll = 0; ll < cta; ll++)
                                GetFaceData(m.m_Primitives[index % m.m_Primitives.Count], new Vector2(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f)), positions, normals, colors, uvs);
                            break;
                    }
                }
            }
        }

        static partial class Contents
        {
            public static GUIContent meshBakeMode = new GUIContent("Bake Mode");
            public static GUIContent distribution = new GUIContent("Distribution");
            public static GUIContent mesh = new GUIContent("Mesh");
        }
    }
}
