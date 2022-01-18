using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace UnityEditor.Experimental.VFX.Utility
{
    partial class PointCacheBakeTool : EditorWindow
    {
        public enum MeshBakeMode
        {
            Vertex,
            Triangle
        }

        public enum Distribution
        {
            Sequential,
            Random,
            RandomUniformArea
        }

        Distribution m_Distribution = Distribution.RandomUniformArea;
        MeshBakeMode m_MeshBakeMode = MeshBakeMode.Triangle;
        PCache.Format m_OutputFormat = PCache.Format.Ascii;

        bool m_ExportPositions = true;
        bool m_ExportUV = false;
        bool m_ExportNormals = false;
        bool m_ExportColors = false;
        bool m_ExportBarycentric = false;

        Mesh m_Mesh;
        int m_OutputPointCount = 4096;
        int m_SeedMesh;

        bool m_UseDensityMap = false;
        Texture2D m_DensityMap;

        void OnGUI_Mesh()
        {
            GUILayout.Label("Mesh Baking", EditorStyles.boldLabel);
            m_Mesh = (Mesh)EditorGUILayout.ObjectField(Contents.mesh, m_Mesh, typeof(Mesh), false);
            m_Distribution = (Distribution)EditorGUILayout.EnumPopup(Contents.distribution, m_Distribution);
            if (m_Distribution != Distribution.RandomUniformArea)
                m_MeshBakeMode = (MeshBakeMode)EditorGUILayout.EnumPopup(Contents.meshBakeMode, m_MeshBakeMode);

            if (m_Distribution == Distribution.RandomUniformArea)
            {
                m_UseDensityMap = EditorGUILayout.Toggle("Use Density Map", m_UseDensityMap);
                if (m_UseDensityMap)
                    m_DensityMap = (Texture2D)EditorGUILayout.ObjectField("Density Map", m_DensityMap, typeof(Texture2D), false);
            }

            m_ExportPositions = EditorGUILayout.Toggle("Export Positions", m_ExportPositions);
            m_ExportNormals = EditorGUILayout.Toggle("Export Normals", m_ExportNormals);
            m_ExportColors = EditorGUILayout.Toggle("Export Colors", m_ExportColors);
            m_ExportUV = EditorGUILayout.Toggle("Export UVs", m_ExportUV);
            if (m_MeshBakeMode == MeshBakeMode.Triangle)
                m_ExportBarycentric = EditorGUILayout.Toggle("Export Barycentric", m_ExportBarycentric);

            m_OutputPointCount = EditorGUILayout.IntField("Point Count", m_OutputPointCount);
            if (m_Distribution != Distribution.Sequential)
                m_SeedMesh = EditorGUILayout.IntField("Seed", m_SeedMesh);

            m_OutputFormat = (PCache.Format)EditorGUILayout.EnumPopup("File Format", m_OutputFormat);

            if (m_Mesh != null)
            {
                if (GUILayout.Button("Save to pCache file..."))
                {
                    var fileName = EditorUtility.SaveFilePanelInProject("pCacheFile", m_Mesh.name, "pcache", "Save PCache");
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        try
                        {
                            EditorUtility.DisplayProgressBar("pCache bake tool", "Capturing data...", 0.0f);
                            var file = ComputePCacheFromMesh();
                            if (file != null)
                            {
                                EditorUtility.DisplayProgressBar("pCache bake tool", "Saving pCache file", 1.0f);
                                file.SaveToFile(fileName, m_OutputFormat);
                            }
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogException(e);
                        }
                        EditorUtility.ClearProgressBar();
                    }
                }
                EditorGUILayout.Space();

                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("Mesh Statistics", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    var saveEnabled = GUI.enabled;
                    GUI.enabled = false;
                    EditorGUILayout.IntField("Vertices", m_Mesh.vertexCount);
                    EditorGUILayout.IntField("Triangles", m_Mesh.triangles.Length);
                    EditorGUILayout.IntField("Sub Meshes", m_Mesh.subMeshCount);
                    GUI.enabled = saveEnabled;
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

                public static Vertex operator +(Vertex a, Vertex b)
                {
                    if (a.uvs.Length != b.uvs.Length)
                        throw new InvalidOperationException("Adding compatible vertex");

                    var r = new Vertex()
                    {
                        position = a.position + b.position,
                        color = a.color + b.color,
                        normal = a.normal + b.normal,
                        tangent = a.tangent + b.tangent,
                        uvs = new Vector4[a.uvs.Length]
                    };

                    for (int i = 0; i < a.uvs.Length; ++i)
                        r.uvs[i] = a.uvs[i] + b.uvs[i];

                    return r;
                }

                public static Vertex operator *(float a, Vertex b)
                {
                    var r = new Vertex()
                    {
                        position = a * b.position,
                        color = a * b.color,
                        normal = a * b.normal,
                        tangent = a * b.tangent,
                        uvs = new Vector4[b.uvs.Length]
                    };

                    for (int i = 0; i < b.uvs.Length; ++i)
                        r.uvs[i] = a * b.uvs[i];

                    return r;
                }
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
            public struct Result
            {
                public MeshData.Vertex vertex;
                public Vector2 barycentric;
                public int triangleIndex;
            }

            public abstract Result GetNext();

            protected Picker(MeshData data)
            {
                m_cacheData = data;
            }

            //See http://inis.jinr.ru/sl/vol1/CMC/Graphics_Gems_1,ed_A.Glassner.pdf (p24) uniform distribution from two numbers in triangle generating barycentric coordinate
            protected readonly static Vector2 center_of_sampling = new Vector2(4.0f / 9.0f, 3.0f / 4.0f);
            protected MeshData.Vertex Interpolate(MeshData.Triangle triangle, Vector2 p)
            {
                return Interpolate(m_cacheData.vertices[triangle.a], m_cacheData.vertices[triangle.b], m_cacheData.vertices[triangle.c], p);
            }

            protected static Vector3 BarycentricFromSquare(Vector2 p)
            {
                float s = p.x;
                float t = Mathf.Sqrt(p.y);
                float a = 1.0f - t;
                float b = (1 - s) * t;
                float c = s * t;
                return new Vector3(a, b, c);
            }

            protected static MeshData.Vertex Interpolate(MeshData.Vertex A, MeshData.Vertex B, MeshData.Vertex C, Vector2 p)
            {
                var bary = BarycentricFromSquare(p);

                var r = bary.x * A + bary.y * B + bary.z * C;
                r.normal = r.normal.normalized;
                var tangent = new Vector3(r.tangent.x, r.tangent.y, r.tangent.z).normalized;
                r.tangent = new Vector4(tangent.x, tangent.y, tangent.z, r.tangent.w > 0.0f ? 1.0f : -1.0f);
                return r;
            }

            protected MeshData m_cacheData;
        }

        abstract class RandomPicker : Picker
        {
            protected RandomPicker(MeshData data, System.Random rand) : base(data)
            {
                m_Rand = rand;
            }

            protected float GetNextRandFloat()
            {
                return (float)m_Rand.NextDouble(); //[0; 1[
            }

            protected System.Random m_Rand;
        }

        class RandomPickerVertex : RandomPicker
        {
            public RandomPickerVertex(MeshData data, System.Random rand) : base(data, rand)
            {
            }

            public sealed override Result GetNext()
            {
                var randomIndex = m_Rand.Next(0, m_cacheData.vertices.Length);
                return new Result(){ vertex = m_cacheData.vertices[randomIndex] };
            }
        }

        class SequentialPickerVertex : Picker
        {
            private uint m_Index = 0;
            public SequentialPickerVertex(MeshData data) : base(data)
            {
            }

            public sealed override Result GetNext()
            {
                var r = m_cacheData.vertices[m_Index];
                m_Index++;
                if (m_Index >= m_cacheData.vertices.Length)
                    m_Index = 0;
                return new Result() { vertex = r };
            }
        }

        class RandomPickerTriangle : RandomPicker
        {
            public RandomPickerTriangle(MeshData data, System.Random rand) : base(data, rand)
            {
            }

            public sealed override Result GetNext()
            {
                var index = m_Rand.Next(0, m_cacheData.triangles.Length);
                var rand = new Vector2(GetNextRandFloat(), GetNextRandFloat());
                return new Result()
                {
                    vertex = Interpolate(m_cacheData.triangles[index], rand),
                    triangleIndex = index,
                    barycentric = rand
                };
            }
        }

        class RandomPickerUniformArea : RandomPicker
        {
            private readonly double[] m_accumulatedAreaTriangles;

            private double ComputeTriangleArea(MeshData.Triangle t)
            {
                var A = m_cacheData.vertices[t.a].position;
                var B = m_cacheData.vertices[t.b].position;
                var C = m_cacheData.vertices[t.c].position;
                return 0.5f * Vector3.Cross(B - A, C - A).magnitude;
            }

            public RandomPickerUniformArea(MeshData data, System.Random rand) : base(data, rand)
            {
                m_accumulatedAreaTriangles = new double[data.triangles.Length];
                m_accumulatedAreaTriangles[0] = ComputeTriangleArea(data.triangles[0]);
                for (int i = 1; i < data.triangles.Length; ++i)
                {
                    m_accumulatedAreaTriangles[i] = m_accumulatedAreaTriangles[i - 1] + ComputeTriangleArea(data.triangles[i]);
                }
            }

            private uint FindIndexOfArea(double area)
            {
                uint min = 0;
                uint max = (uint)m_accumulatedAreaTriangles.Length - 1;
                uint mid = max >> 1;
                while (max >= min)
                {
                    if (mid > m_accumulatedAreaTriangles.Length)
                        throw new InvalidOperationException("Cannot Find FindIndexOfArea");

                    if (m_accumulatedAreaTriangles[mid] >= area &&
                        (mid == 0 || (m_accumulatedAreaTriangles[mid - 1] < area)))
                    {
                        return mid;
                    }
                    else if (area < m_accumulatedAreaTriangles[mid])
                    {
                        max = mid - 1;
                    }
                    else
                    {
                        min = mid + 1;
                    }
                    mid = (min + max) >> 1;
                }
                throw new InvalidOperationException("Cannot FindIndexOfArea");
            }

            public sealed override Result GetNext()
            {
                var areaPosition = m_Rand.NextDouble() * m_accumulatedAreaTriangles.Last();
                var areaIndex = FindIndexOfArea(areaPosition);
                var rand = new Vector2(GetNextRandFloat(), GetNextRandFloat());

                return new Result()
                {
                    vertex = Interpolate(m_cacheData.triangles[areaIndex], rand),
                    barycentric	= BarycentricFromSquare(rand),
                    triangleIndex = (int)areaIndex
                };
            }
        }

        class SequentialPickerTriangle : Picker
        {
            private uint m_Index = 0;
            public SequentialPickerTriangle(MeshData data) : base(data)
            {
            }

            public override sealed Result GetNext()
            {
                var t = m_cacheData.triangles[m_Index];
                m_Index++;
                if (m_Index >= m_cacheData.triangles.Length)
                    m_Index = 0;
                return new Result()
                {
                    vertex = Interpolate(t, center_of_sampling),
                    barycentric = center_of_sampling,
                    triangleIndex = (int)m_Index
                };
            }
        }

        PCache ComputePCacheFromMesh()
        {
            var meshCache = ComputeDataCache(m_Mesh);

            var rand = new System.Random(m_SeedMesh);
            Picker picker = null;
            if (m_Distribution == Distribution.Sequential)
            {
                if (m_MeshBakeMode == MeshBakeMode.Vertex)
                {
                    picker = new SequentialPickerVertex(meshCache);
                }
                else if (m_MeshBakeMode == MeshBakeMode.Triangle)
                {
                    picker = new SequentialPickerTriangle(meshCache);
                }
            }
            else if (m_Distribution == Distribution.Random)
            {
                if (m_MeshBakeMode == MeshBakeMode.Vertex)
                {
                    picker = new RandomPickerVertex(meshCache, rand);
                }
                else if (m_MeshBakeMode == MeshBakeMode.Triangle)
                {
                    picker = new RandomPickerTriangle(meshCache, rand);
                }
            }
            else if (m_Distribution == Distribution.RandomUniformArea)
            {
                picker = new RandomPickerUniformArea(meshCache, rand);
            }
            if (picker == null)
                throw new InvalidOperationException("Unable to find picker");

            var exportBarycentric = m_ExportBarycentric && m_MeshBakeMode == MeshBakeMode.Triangle;

            var positions = new List<Vector3>();
            var normals = m_ExportNormals ? new List<Vector3>() : null;
            var colors = m_ExportColors ? new List<Vector4>() : null;
            var uvs = m_ExportUV ? new List<Vector4>() : null;
            var barycentrics = exportBarycentric ? new List<(Vector2 coord, int triangle)>() : null;

            //bool restoreDensityToNotReadable = false; //TODOPAUL
            bool useDensityMap = m_Distribution == Distribution.RandomUniformArea && m_UseDensityMap && m_DensityMap != null;
            if (useDensityMap)
            {
                if (!m_DensityMap.isReadable)
                {
                    var path = AssetDatabase.GetAssetPath(m_DensityMap);
                    var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                    //var backupReadable = importer.isReadable;
                    importer.isReadable = true;
                    importer.SaveAndReimport();
                    //pixels = m_Texture.GetPixels();
                    //importer.isReadable = backupReadable;
                    //importer.SaveAndReimport();
                }

                if (!meshCache.vertices[0].uvs.Any())
                    throw new InvalidOperationException("Expecting UV0");
            }

            int iteration = 0;
            while(positions.Count < m_OutputPointCount)
            {
                if (iteration-- < 0)
                {
                    iteration = 1024;
                    var cancel = EditorUtility.DisplayCancelableProgressBar("pCache bake tool", string.Format("Sampling data... {0}/{1}", positions.Count, m_OutputPointCount), (float)positions.Count / (float)m_OutputPointCount);
                    if (cancel)
                    {
                        //TODOPAUL take consideration of density map update
                        return null;
                    }
                }

                var r = picker.GetNext();
                if (useDensityMap)
                {
                    var sampledColor = m_DensityMap.GetPixelBilinear(r.vertex.uvs[0].x, r.vertex.uvs[0].y);
                    var draw = (float)rand.NextDouble();
                    if (draw >= sampledColor.r)
                    {
                        //Skip this sampling
                        continue;
                    }
                }

                positions.Add(r.vertex.position);
                if (m_ExportNormals) normals.Add(r.vertex.normal);
                if (m_ExportColors) colors.Add(r.vertex.color);
                if (m_ExportUV) uvs.Add(r.vertex.uvs.Any() ? r.vertex.uvs[0] : Vector4.zero);
                if (exportBarycentric) barycentrics.Add((r.barycentric, r.triangleIndex));
            }

            var file = new PCache();
            if (m_ExportPositions) file.AddVector3Property("position");
            if (m_ExportNormals) file.AddVector3Property("normal");
            if (m_ExportColors) file.AddColorProperty("color");
            if (m_ExportUV) file.AddVector4Property("uv");
            if (exportBarycentric)
            {
                file.AddIntegerProperty("triangleIndex");
                file.AddVector2Property("barycentric");
            }

            EditorUtility.DisplayProgressBar("pCache bake tool", "Generating pCache...", 0.0f);
            if (m_ExportPositions) file.SetVector3Data("position", positions);
            if (m_ExportNormals) file.SetVector3Data("normal", normals);
            if (m_ExportColors) file.SetColorData("color", colors);
            if (m_ExportUV) file.SetVector4Data("uv", uvs);
            if (exportBarycentric)
            {
                file.SetIntData("triangleIndex", barycentrics.Select(o => o.triangle).ToList());
                file.SetVector2Data("barycentric", barycentrics.Select(o => o.coord).ToList());
            }

            EditorUtility.ClearProgressBar();
            return file;
        }

        static partial class Contents
        {
            public static GUIContent meshBakeMode = new GUIContent("Bake Mode");
            public static GUIContent distribution = new GUIContent("Distribution");
            public static GUIContent mesh = new GUIContent("Mesh");
        }
    }
}
