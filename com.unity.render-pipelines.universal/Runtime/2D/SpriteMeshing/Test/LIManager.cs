using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering.Universal
{
    [ExecuteInEditMode]
    public class LIManager : MonoBehaviour
    {
        public Texture2D m_Input;
        public float m_MinimumArea = 4;
        [Range(0, 255)]
        public short m_AlphaCutoff;
        public bool m_Generate;

        public int m_InspectVertex = -1;
        public Vector2 m_InspectedVertexPos;

        public string m_OutputPath;
        public Vector2Int m_Resolution;
        public bool m_TestLineDrawing;

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

                        if (shape.m_IsOpaque)
                            Debug.DrawLine(curPt, nextPt, opaqueColor);
                        else
                            Debug.DrawLine(curPt, nextPt, transparentColor);
                    }
                }
            }
        }

        void TestLineDrawing()
        {
            Texture2D outputTex = new Texture2D(m_Resolution.x, m_Resolution.y);
            LineIntersectionManager liManager = new LineIntersectionManager(outputTex.width, outputTex.height);
            liManager.TestLineDrawing(outputTex);

            byte[] png = outputTex.EncodeToPNG();

            File.WriteAllBytes(m_OutputPath + "lines.png", png);
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

        void TestIntersectionManager(ShapeLibrary shapeLibrary)
        {
            Vector2 start = new Vector2(1, 0);
            Vector2 end = new Vector2(shapeLibrary.m_Region.width - 1, shapeLibrary.m_Region.height);
            Debug.DrawLine(start, end, Color.white);


            //shapeLibrary.m_LineIntersectionManager.DebugCellContents(0, 0, Color.magenta, Color.clear, LineIntersectionManager.DebugCellContentsMode.IncrementingColor);
            bool intersection = shapeLibrary.m_LineIntersectionManager.HasIntersection(new uint[0], start, end, true);
            //shapeLibrary.m_LineIntersectionManager.DebugCellContents(0, 0, Color.magenta, Color.clear);
        }


        // Update is called once per frame
        void Update()
        {
            RectInt region = new RectInt(0, 0, m_Input.width, m_Input.height);


            if (m_Generate)
            {
                m_Generate = false;

                DestroyChildren();
                m_ShapeLibrary.SetRegion(region);
                GenerateMeshes.MakeShapes(m_ShapeLibrary, m_Input, m_AlphaCutoff, m_MinimumArea);
            }


            if (m_ShapeLibrary.m_Shapes.Count > 0)
            {
                //m_ShapeLibrary.m_LineIntersectionManager.DebugDrawCellGrid(Color.gray);
                DebugDrawShapes(m_ShapeLibrary, 0.25f, Color.red, Color.green);
                //TestIntersectionManager(m_ShapeLibrary);
            }

            if (m_InspectVertex >= 0)
            {
                int index = 0;
                foreach (ContourData contourData in m_ShapeLibrary.m_ContourData.Values)
                {
                    for (int i = 0; i < contourData.m_Vertices.Count; i++)
                    {
                        if (index == m_InspectVertex)
                        {
                            DrawCross(contourData.m_Vertices[i], 2, Color.white);
                            m_InspectedVertexPos = contourData.m_Vertices[i];
                            break;
                        }
                        index++;
                    }
                }
            }


            if (m_TestLineDrawing)
            {
                TestLineDrawing();
                m_TestLineDrawing = false;
            }

        }
    }
}
