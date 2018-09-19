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
            //Edge,
            Triangle
        }

        public enum Distribution
        {
            Sequential,
            Random,
            //RandomPerUnit
        }

        Distribution m_Distribution = Distribution.Random;
        MeshBakeMode m_MeshBakeMode = MeshBakeMode.Triangle;
        PCache.Format m_OutputFormat = PCache.Format.Ascii;

        bool m_ExportUV = false;
        bool m_ExportNormals = true;
        bool m_ExportColors = false;
        bool m_UniformPrepass = true;

        Mesh m_Mesh;
        int m_OutputPointCount = 4096;
        int m_Seed;

        void OnGUI_Mesh()
        {
            GUILayout.Label("Mesh Baking", EditorStyles.boldLabel);
            m_Mesh = (Mesh)EditorGUILayout.ObjectField(Contents.mesh, m_Mesh, typeof(Mesh), false);
            m_MeshBakeMode = (MeshBakeMode)EditorGUILayout.EnumPopup(Contents.meshBakeMode, m_MeshBakeMode);
            m_Distribution = (Distribution)EditorGUILayout.EnumPopup(Contents.distribution, m_Distribution);

            m_ExportNormals = EditorGUILayout.Toggle("Export Normals", m_ExportNormals);
            m_ExportColors = EditorGUILayout.Toggle("Export Colors", m_ExportColors);
            m_ExportUV = EditorGUILayout.Toggle("Export UVs", m_ExportUV);

            m_OutputPointCount = EditorGUILayout.IntField("Point Count", m_OutputPointCount);
            if (m_Distribution != Distribution.Sequential)
                m_Seed = EditorGUILayout.IntField("Seed", m_Seed);

            m_OutputFormat = (PCache.Format)EditorGUILayout.EnumPopup("File Format", m_OutputFormat);

            if (m_Mesh != null)
            {
                if (GUILayout.Button("Save to pCache file..."))
                {
                    string fileName = EditorUtility.SaveFilePanelInProject("pCacheFile", m_Mesh.name, "pcache", "Save PCache");
                    if (fileName != null)
                    {

                        try
                        {
                            EditorUtility.DisplayCancelableProgressBar("pCache bake tool", "Capturing data...", 0.0f);
                            var file = ComputePCacheFromMesh();
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
                    EditorGUILayout.IntField("Vertices", m_Mesh.vertexCount);
                    EditorGUILayout.IntField("Triangles", m_Mesh.triangles.Length);
                    EditorGUILayout.IntField("Sub Meshes", m_Mesh.subMeshCount);
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.Space();
        }

        class MeshData
        {
            public struct Vertex
            {
                public Vector3 position;
                public Color color;
                public Vector3 normal;
                public Vector4 tangent;
                public Vector4[] uvs;
            };

            public struct Triangle
            {
                public int a, b, c;
            };

            public Vertex[] vertices;
            public Triangle[] triangles;
        }

        static MeshData ComputeDataCache(Mesh input)
        {
            var positions = input.vertices;
            var normals = input.normals;
            var tangents = input.tangents;
            var colors = input.colors;
            var uvs = new List<Vector4[]>();

            normals = normals.Length == input.vertexCount ? normals : null;
            tangents = tangents.Length == input.vertexCount ? tangents : null;
            colors = colors.Length == input.vertexCount ? colors : null;

            for (int i = 0; i < 8; ++i)
            {
                var uv = new List<Vector4>();
                input.GetUVs(i, uv);
                if (uv.Count == input.vertexCount)
                {
                    uvs.Add(uv.ToArray());
                }
                else
                {
                    break;
                }
            }

            var meshData = new MeshData();
            meshData.vertices = new MeshData.Vertex[input.vertexCount];
            for (int i = 0; i < input.vertexCount; ++i)
            {
                meshData.vertices[i] = new MeshData.Vertex()
                {
                    position = positions[i],
                    color = colors != null ? colors[i] : Color.white,
                    normal = normals != null ? normals[i] : Vector3.up,
                    tangent = tangents != null ? tangents[i] : Vector4.one,
                    uvs = Enumerable.Range(0, uvs.Count).Select(c => uvs[c][i]).ToArray()
                };
            }

            meshData.triangles = new MeshData.Triangle[input.triangles.Length / 3];
            var triangles = input.triangles;
            for (int i = 0; i < meshData.triangles.Length; ++i)
            {
                meshData.triangles[i] = new MeshData.Triangle()
                {
                    a = triangles[i * 3 + 0],
                    b = triangles[i * 3 + 1],
                    c = triangles[i * 3 + 2],
                };
            }
            return meshData;
        }

        abstract class Picker
        {
            public abstract MeshData.Vertex GetNext();

            protected Picker (MeshData data)
            {
                m_cacheData = data;
            }
            protected MeshData m_cacheData;
        }

        abstract class RandomPicker : Picker
        {
            protected RandomPicker(MeshData data, int seed) : base(data)
            {
                m_Rand = new System.Random(seed);
            }

            protected float GetNextRandFloat()
            {
                return (float)m_Rand.NextDouble(); //[0; 1[
            }

            protected System.Random m_Rand;
        }

        class RandomPickerVertex : RandomPicker
        {
            public RandomPickerVertex(MeshData data, int seed) : base(data, seed)
            {

            }

            public override sealed MeshData.Vertex GetNext()
            {
                int randomIndex = m_Rand.Next(0, m_cacheData.vertices.Length);
                return m_cacheData.vertices[randomIndex];
            }
        }

        PCache ComputePCacheFromMesh()
        {
            var file = new PCache();
            file.AddVector3Property("position");
            if (m_ExportNormals) file.AddVector3Property("normal");
            if (m_ExportColors) file.AddColorProperty("color");
            if (m_ExportUV) file.AddVector2Property("uv");

            var meshCache = ComputeDataCache(m_Mesh);

            return file;
        }


        float[] m_EdgeLength;
        float[] m_TriangleArea;

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

        /*
        void GetData(EasyMesh m, int count, Distribution distribution, MeshBakeMode bakeMode, List<Vector3> positions, List<Vector3> normals = null, List<Vector4> colors = null, List<Vector2> uvs = null)
        {
            int total = count;

            if (distribution != Distribution.Random)
            {
                switch (bakeMode)
                {
                    case MeshBakeMode.Vertex: total = m.m_Vertices.Count; break;
                    //case MeshBakeMode.Edge: total = m.m_Edges.Count; break;
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
                        //case MeshBakeMode.Edge: GetEdgeData(m.m_Edges[index % m.m_Edges.Count], Random.Range(0.0f, 1.0f), positions, normals, colors, uvs); break;
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
        */

        static partial class Contents
        {
            public static GUIContent meshBakeMode = new GUIContent("Bake Mode");
            public static GUIContent distribution = new GUIContent("Distribution");
            public static GUIContent mesh = new GUIContent("Mesh");
        }
    }
}
