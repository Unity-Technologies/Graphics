using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace UnityEngine.Experimental.Rendering.Universal
{
    [ExecuteInEditMode]
    public class PipelineTest : MonoBehaviour
    {
        public Texture2D m_Input;
        public float m_MinimumArea = 4;
        [Range(0, 255)]
        public short m_AlphaCutoff;
        public Material m_OpaqueMaterial;
        public Material m_TransparentMaterial;
        public bool m_Clear;
        public bool m_Generate;
        public bool m_Tesselate;
        public bool m_FalseColoring;
        public bool m_DrawShapes = true;
        public bool m_TestIntersection = false;
        public float m_CrossScale = 0.25f;


        public int m_Vertices;
        public int m_Triangles;

        public int m_TransparentArea = 0;
        public int m_OpaqueArea = 0;
        public float m_OpaqueCoverage = 0.0f;
        public float m_ReducedTransparency = 0.0f;

        Texture2D m_PrevInput = null;
        float m_PreviousMinimumArea = 4;
        bool m_PrevFalseColoring;
        bool m_ClearTesselation = false;


        ShapeLibrary m_ShapeLibrary = new ShapeLibrary();


        static void DrawCross(Vector2 point, float size, Color color)
        {
            Debug.DrawLine(point + Vector2.up * size, point + Vector2.down * size, color);
            Debug.DrawLine(point + Vector2.left * size, point + Vector2.right * size, color);
        }

        void DebugDrawShapes(ShapeLibrary shapeLibrary, float crossScale, Color opaqueColor, Color transparentColor)
        {
            if (shapeLibrary == null)
                return;

            foreach (Shape shape in shapeLibrary.m_Shapes)
            {
                // Draw Contours
                foreach (Contour contour in shape.m_Contours)
                {
                    List<Vector2> shapePath = contour.m_ContourData.m_Vertices;
                    for (int i = 0; i < shapePath.Count; i++)
                    {
                        Vector2 curPt = shapePath[i];
                        Vector2 nextPt = shapePath[(i + 1) % shapePath.Count];

                        if (i == 0)
                            DrawCross(curPt, crossScale, Color.white);
                        else if (i == 1)
                            DrawCross(curPt, crossScale, Color.yellow);


                        if (shape.m_IsOpaque)
                            Debug.DrawLine(curPt, nextPt, opaqueColor);
                        else
                            Debug.DrawLine(curPt, nextPt, transparentColor);
                    }
                }
            }
        }

        void CreateMeshes(ShapeLibrary shapeLib, Material opaqueMaterial, Material transparentMaterial)
        {
            int shapeCount = 0;

            GenerateMeshes.TesselateShapes(shapeLib, (vertices, triangles, uvs, isOpaque) =>
            {
                string objectName = isOpaque ? "Opaque" : "Transparent";

                GameObject go = new GameObject(objectName + " " + shapeCount);
                go.transform.parent = transform;
                MeshFilter mf = go.AddComponent<MeshFilter>();
                MeshRenderer mr = go.AddComponent<MeshRenderer>();
                UnityEngine.Mesh mesh = new UnityEngine.Mesh();

                if (isOpaque)
                    mr.material = opaqueMaterial;
                else
                    mr.material = transparentMaterial;

                mesh.Clear();
                mesh.vertices = vertices;
                mesh.SetIndices(triangles, MeshTopology.Triangles, 0);
                mesh.uv = uvs;

                mf.sharedMesh = mesh;

                m_Vertices += vertices.Length;
                m_Triangles += triangles.Length / 3;

                shapeCount++;
            });
        }

        void DestroyChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                child.parent = null;
                GameObject go = child.gameObject;

                DestroyImmediate(go, true);
            }
        }

        float ComputeArea(List<Vector2> vertices)
        {
            float area = 0;
            Vector2 prevPoint = vertices[vertices.Count - 1];
            for (int i = 0; i < vertices.Count; i++)
            {
                Vector2 curPoint = vertices[i];
                area += (int)prevPoint.x * (int)curPoint.y - (int)curPoint.x * (int)prevPoint.y;
                prevPoint = curPoint;
            }

            return 0.5f * Mathf.Abs(area);
        }


        void TestIntersectionManager(ShapeLibrary shapeLibrary)
        {
            Vector2 start = new Vector2(100, 0);
            Vector2 end = new Vector2(shapeLibrary.m_Region.width - 100, shapeLibrary.m_Region.height);
            Debug.DrawLine(start, end, Color.white);
            bool intersection = shapeLibrary.m_LineIntersectionManager.HasIntersection(new uint[0], start, end, true);
        }

        // Update is called once per frame
        void Update()
        {
            if (!m_Tesselate)
                m_FalseColoring = false;

            RectInt region = new RectInt(0, 0, m_Input.width, m_Input.height);

            if (m_Clear || m_PrevInput != m_Input)
            {
                DestroyChildren();
                m_Clear = false;
                m_Tesselate = false;
                m_PrevInput = m_Input;
                m_DrawShapes = true;
            }

            if (m_Generate)
            {
                m_Generate = false;

                DestroyChildren();

                System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

                m_ShapeLibrary.SetRegion(region);
                GenerateMeshes.MakeShapes(m_ShapeLibrary, m_Input, m_AlphaCutoff, m_MinimumArea);

                m_OpaqueMaterial = new Material(m_OpaqueMaterial);
                m_TransparentMaterial = new Material(m_TransparentMaterial);
                m_OpaqueMaterial.SetTexture("_MainTex", m_Input);
                m_TransparentMaterial.SetTexture("_MainTex", m_Input);

                m_Vertices = 0;
                m_Triangles = 0;

                CreateMeshes(m_ShapeLibrary, m_OpaqueMaterial, m_TransparentMaterial);

                stopwatch.Stop();
                Debug.Log("Time: " + (float)stopwatch.ElapsedMilliseconds / 1000.0f);

                m_Tesselate = false;
                m_DrawShapes = true;

                float opaqueArea = 0;
                float transparentArea = 0;
                foreach (Shape shape in m_ShapeLibrary.m_Shapes)
                {
                    float shapeArea = 0;
                    foreach (Contour contour in shape.m_Contours)
                    {
                        if (contour.m_IsOuterEdge)
                            shapeArea += ComputeArea(contour.m_ContourData.m_Vertices);
                    }

                    if (shape.m_IsOpaque)
                        opaqueArea += shapeArea;
                    else
                        transparentArea += shapeArea;
                }

                m_OpaqueArea = (int)opaqueArea;
                m_TransparentArea = (int)transparentArea;
                m_OpaqueCoverage = opaqueArea / transparentArea;

                float width = m_Input.width;
                float height = m_Input.height;
                m_ReducedTransparency = (float)(((width * height) - m_TransparentArea) + m_OpaqueArea) / (float)(width * height);
            }

            if (m_Tesselate)
            {
                if (m_PreviousMinimumArea != m_MinimumArea || !m_ClearTesselation)
                {
                    DestroyChildren();


                    GenerateMeshes.ReduceVertices(m_ShapeLibrary, m_MinimumArea);

                    m_OpaqueMaterial = new Material(m_OpaqueMaterial);
                    m_TransparentMaterial = new Material(m_TransparentMaterial);
                    m_OpaqueMaterial.SetTexture("_MainTex", m_Input);
                    m_TransparentMaterial.SetTexture("_MainTex", m_Input);

                    m_Vertices = 0;
                    m_Triangles = 0;

                    CreateMeshes(m_ShapeLibrary, m_OpaqueMaterial, m_TransparentMaterial);

                    m_ClearTesselation = true;
                }
            }
            else
            {
                if (m_PreviousMinimumArea != m_MinimumArea)
                    GenerateMeshes.ReduceVertices(m_ShapeLibrary, m_MinimumArea);

                if (m_ClearTesselation)
                {
                    DestroyChildren();
                    m_ClearTesselation = false;
                    m_DrawShapes = true;
                }
            }

            if (m_DrawShapes)
                DebugDrawShapes(m_ShapeLibrary, m_CrossScale, Color.red, Color.green);

            //if(m_TestIntersection)
            //    TestIntersectionManager(m_ShapeLibrary);


            m_PreviousMinimumArea = m_MinimumArea;
        }
    }
}
