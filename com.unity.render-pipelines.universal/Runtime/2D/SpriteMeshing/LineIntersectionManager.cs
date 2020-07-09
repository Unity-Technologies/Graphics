using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

namespace UnityEngine.Experimental.Rendering.Universal
{
    internal class LineIntersectionManager
    {
        public enum DebugCellContentsMode
        {
            GivenColor,
            IncrementingColor,
        }


        static int m_CellSizeBits = 0;
        static int m_CellSize = 1 << m_CellSizeBits;  // Cell size is 1x1. This means more memory usage.

        class Line
        {
            public uint id;
            public float2 start;
            public float2 end;

            public Line(uint lineId, float2 lineStart, float2 lineEnd)
            {
                id = lineId;
                start = lineStart;
                end = lineEnd;
            }
        }

        Dictionary<uint, Line>[,] m_AllLines = null;
        int m_WidthInPixels;
        int m_HeightInPixels;
        int m_WidthInCells;
        int m_HeightInCells;

        public LineIntersectionManager(int pixelWidth, int pixelHeight)
        {
            m_WidthInCells = (pixelWidth >> m_CellSizeBits);
            m_HeightInCells = (pixelHeight >> m_CellSizeBits);

            m_AllLines = new Dictionary<uint, Line>[m_WidthInCells + 1, m_HeightInCells + 1];
        }

        public void DebugElements()
        {
            for (int y = 0; y < m_HeightInCells; y++)
            {
                for (int x = 0; x < m_WidthInCells; x++)
                {
                    Dictionary<uint, Line> lines = m_AllLines[x, y];
                    if (lines != null && lines.Values.Count > 0)
                    {
                        foreach (Line line in lines.Values)
                        {
                            Debug.DrawLine(new Vector3(line.start.x, line.start.y), new Vector3(line.end.x, line.end.y), Color.blue);
                        }
                    }
                }
            }
        }

        void DoLineOperation(int2 start, int2 end, Action<int2> lineHandler)
        {
            // Uses Breshenham's line drawing operate on points on the line
            int xSize = end.x - start.x;
            int ySize = end.y - start.y;

            int incrementX1 = 0;
            int incrementY1 = 0;
            int incrementX2 = 0;
            int incrementY2 = 0;

            if (xSize < 0)
                incrementX1 = -1;
            else if (xSize > 0)
                incrementX1 = 1;

            if (ySize < 0)
                incrementY1 = -1;
            else if (ySize > 0)
                incrementY1 = 1;

            int largestDelta = Math.Abs(xSize);
            int smallestDelta = Math.Abs(ySize);
            if (largestDelta <= smallestDelta)
            {
                int tmp = largestDelta;
                largestDelta = smallestDelta;
                smallestDelta = tmp;

                if (ySize < 0)
                    incrementY2 = -1;
                else if (ySize > 0)
                    incrementY2 = 1;
            }
            else
            {
                if (xSize < 0)
                    incrementX2 = -1;
                else if (xSize > 0)
                    incrementX2 = 1;
            }

            int2 curPos = start;
            int error = largestDelta >> 1;
            for (int i = 0; i <= largestDelta; i++)
            {
                lineHandler(curPos);

                error += smallestDelta;
                if (error >= largestDelta)
                {
                    error -= largestDelta;
                    curPos.x += incrementX1;
                    curPos.y += incrementY1;
                }
                else
                {
                    curPos.x += incrementX2;
                    curPos.y += incrementY2;
                }
            }
        }

        public void AddLine(Vector2 start, Vector2 end)
        {
            AddLine(new float2(start.x, start.y), new float2(end.x, end.y));
        }

        public void AddLine(float2 start, float2 end)
        {
            uint lineId = LineHash(start, end);
            Line line = new Line(lineId, start, end);

            int x1 = (int)start.x >> m_CellSizeBits;
            int y1 = (int)start.y >> m_CellSizeBits;
            int x2 = (int)end.x >> m_CellSizeBits;
            int y2 = (int)end.y >> m_CellSizeBits;

            DoLineOperation(new int2(x1, y1), new int2(x2, y2), (pos) =>
            {
                if (pos.x >= 0 && pos.y >= 0 && pos.x <= m_WidthInCells && pos.y <= m_HeightInCells)
                {
                    Dictionary<uint, Line> lines = m_AllLines[pos.x, pos.y];
                    if (lines == null)
                        lines = new Dictionary<uint, Line>();

                    if (!lines.ContainsKey(line.id))
                    {
                        lines.Add(line.id, line);
                        m_AllLines[pos.x, pos.y] = lines;
                    }
                }
            //else
            //{
            //    Debug.Log("Error outside bounds (" + start.x + "," + start.y + ") - (" + end.x + "," + end.y + ")");
            //}
        });
        }


        public void RemoveLine(Vector2 start, Vector2 end)
        {
            RemoveLine(new float2(start.x, start.y), new float2(end.x, end.y));
        }

        public void RemoveLine(float2 start, float2 end)
        {
            uint lineId = LineHash(start, end);

            int x1 = (int)start.x >> m_CellSizeBits;
            int y1 = (int)start.y >> m_CellSizeBits;
            int x2 = (int)end.x >> m_CellSizeBits;
            int y2 = (int)end.y >> m_CellSizeBits;

            DoLineOperation(new int2(x1, y1), new int2(x2, y2), (pos) =>
            {
                if (pos.x >= 0 && pos.y >= 0 && pos.x <= m_WidthInCells && pos.y <= m_HeightInCells)
                {

                    Dictionary<uint, Line> lines = m_AllLines[pos.x, pos.y];
                    if (lines != null)
                    {
                        if (lines.ContainsKey(lineId))
                        {
                            lines.Remove(lineId);
                            m_AllLines[pos.x, pos.y] = lines;
                        }
                    }
                }
                else
                {
                    Debug.Log("Error outside bounds (" + start.x + "," + start.y + ") - (" + end.x + "," + end.y + ")");
                }
            });
        }

        void DrawCross(Vector2 point, float size, Color color)
        {
            Debug.DrawLine(point + Vector2.up * size, point + Vector2.down * size, color);
            Debug.DrawLine(point + Vector2.left * size, point + Vector2.right * size, color);
        }

        bool IsWithinSegment(float2 testPoint, float2 startPt, float2 endPt)
        {
            float smallestX = startPt.x;
            float largestX = endPt.x;
            if (startPt.x > endPt.x)
            {
                smallestX = endPt.x;
                largestX = startPt.x;
            }

            float smallestY = startPt.y;
            float largestY = endPt.y;

            if (startPt.y > endPt.y)
            {
                smallestY = endPt.y;
                largestY = startPt.y;

            }

            return (testPoint.x >= smallestX) && (testPoint.x <= largestX) && (testPoint.y >= smallestY) && (testPoint.y <= largestY);
        }


        public static uint LineHash(Vector2 start, Vector2 end)
        {
            float x0;
            float x1;
            float y0;
            float y1;

            if (start.x < end.x)
            {
                x0 = start.x;
                x1 = end.x;
            }
            else
            {
                x0 = end.x;
                x1 = start.x;
            }

            if (start.y < end.y)
            {
                y0 = start.y;
                y1 = end.y;
            }
            else
            {
                y0 = end.y;
                y1 = start.y;
            }

            float4 newFloat4 = new float4(x0, y0, x1, y1);
            return Hash.GetHash(newFloat4); ;
        }

        bool CheckCellForIntersection(uint[] lineIds, int cellX, int cellY, float2 start, float2 end, bool debugLines)
        {
            if (cellX < 0 || cellX > m_WidthInCells || cellY < 0 || cellY > m_HeightInCells)
                return false;

            Dictionary<uint, Line> lines = m_AllLines[cellX, cellY];
            if (lines != null)
            {
                foreach (KeyValuePair<uint, Line> lineKV in lines)
                {
                    float2 intersectionPt;
                    uint key = lineKV.Key;
                    Line value = lineKV.Value;

                    bool ignoreIntersection = false;
                    for (int i = 0; (i < lineIds.Length) && !ignoreIntersection; i++)
                    {
                        ignoreIntersection |= key == lineIds[i];
                    }

                    if (!ignoreIntersection)
                    {
                        if (OutlineUtility.GetIntersection(start, end, value.start, value.end, out intersectionPt))
                        {

                            if (IsWithinSegment(intersectionPt, start, end) && IsWithinSegment(intersectionPt, value.start, value.end))
                            {
                                //if (debugLines)
                                //{
                                //    DrawCross(intersectionPt, 2, Color.yellow);
                                //    Debug.Log("Intersection Found");
                                //    Debug.DrawLine(new Vector3(start.x, start.y), new Vector3(end.x, end.y), Color.yellow);
                                //    Debug.DrawLine(new Vector3(lineKV.Value.start.x, lineKV.Value.start.y), new Vector3(lineKV.Value.end.x, lineKV.Value.end.y), Color.magenta);
                                //    //Debug.DrawLine(new Vector3(lineKV.Value.start.x, -lineKV.Value.start.y), new Vector3(lineKV.Value.end.x, lineKV.Value.end.y), Color.magenta);
                                //}

                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }


        public bool HasIntersection(uint[] lineIds, float2 start, float2 end, bool debugLines)
        {
            int x1 = (int)start.x >> m_CellSizeBits;
            int y1 = (int)start.y >> m_CellSizeBits;
            int x2 = (int)end.x >> m_CellSizeBits;
            int y2 = (int)end.y >> m_CellSizeBits;


            bool retValue = false;
            DoLineOperation(new int2(x1, y1), new int2(x2, y2), (pos) =>
            {
                retValue = retValue ? true : CheckCellForIntersection(lineIds, pos.x, pos.y, start, end, debugLines);
                retValue = retValue ? true : CheckCellForIntersection(lineIds, pos.x + 1, pos.y, start, end, debugLines);
                retValue = retValue ? true : CheckCellForIntersection(lineIds, pos.x - 1, pos.y, start, end, debugLines);
                retValue = retValue ? true : CheckCellForIntersection(lineIds, pos.x, pos.y + 1, start, end, debugLines);
                retValue = retValue ? true : CheckCellForIntersection(lineIds, pos.x, pos.y - 1, start, end, debugLines);
            });

            return retValue;
        }

        public void TestLineDrawing(Texture2D outputTexture)
        {
            int radius = (outputTexture.width < outputTexture.height ? outputTexture.width : outputTexture.height) >> 2;

            int segments = 32;
            int xOffset = outputTexture.width >> 1;
            int yOffset = outputTexture.height >> 1;

            for (int i = 0; i <= segments; i++)
            {
                float angle1 = (2 * Mathf.PI * (float)i) / (float)segments;
                float angle2 = (2 * Mathf.PI * (float)(i + 1)) / (float)segments;

                int x1 = (int)((float)radius * Mathf.Cos(angle1)) + xOffset;
                int y1 = (int)((float)radius * Mathf.Sin(angle1)) + yOffset;
                int x2 = (int)((float)radius * Mathf.Cos(angle2)) + xOffset;
                int y2 = (int)((float)radius * Mathf.Sin(angle2)) + yOffset;

                DoLineOperation(new int2(x1, y1), new int2(x2, y2), (pos) =>
                {
                    outputTexture.SetPixel(pos.x, pos.y, Color.red);
                });
            }
        }


        public int GetCellSize() { return m_CellSize; }


        public void DebugCellContents(int cellX, int cellY, Color lineColor, Color gridColor, DebugCellContentsMode mode)
        {
            Dictionary<uint, Line> cellContents = m_AllLines[cellX, cellY];

            Vector3 start = new Vector3((float)(cellX << m_CellSizeBits), (float)(cellY << m_CellSizeBits));
            Vector3 end = new Vector3((float)((cellX + 1) << m_CellSizeBits), (float)((cellY + 1) << m_CellSizeBits));
            Debug.DrawLine(start, new Vector3(start.x, end.y), gridColor);
            Debug.DrawLine(start, new Vector3(end.x, start.y), gridColor);
            Debug.DrawLine(end, new Vector3(start.x, end.y), gridColor);
            Debug.DrawLine(end, new Vector3(end.x, start.y), gridColor);


            int colorIndex = 0;
            foreach (Line line in cellContents.Values)
            {
                if (mode == DebugCellContentsMode.IncrementingColor)
                {
                    Color[] colors = new Color[3] { Color.red, Color.green, Color.blue };
                    lineColor = colors[colorIndex];
                    colorIndex = (colorIndex + 1) % colors.Length;
                }


                Debug.DrawLine(new Vector3(line.start.x, line.start.y), new Vector3(line.end.x, line.end.y), lineColor);
            }
        }

        public void DebugDrawCellGrid(Color gridColor)
        {
            for (int y = 0; y < m_HeightInCells + 1; y++)
            {
                for (int x = 0; x < m_WidthInCells + 1; x++)
                {
                    Vector3 start = new Vector3((float)(x << m_CellSizeBits), (float)(y << m_CellSizeBits));
                    Vector3 end = new Vector3((float)((x + 1) << m_CellSizeBits), (float)((y + 1) << m_CellSizeBits));
                    Debug.DrawLine(start, new Vector3(start.x, end.y), gridColor);
                    Debug.DrawLine(start, new Vector3(end.x, start.y), gridColor);
                    Debug.DrawLine(end, new Vector3(start.x, end.y), gridColor);
                    Debug.DrawLine(end, new Vector3(end.x, start.y), gridColor);
                }
            }
        }

        public void GetDebugOutput(RenderTexture outputTexture)
        {
            Texture2D texture = new Texture2D(outputTexture.width, outputTexture.height);

            TestLineDrawing(texture);

            RenderTexture.active = outputTexture;
            Graphics.Blit(texture, outputTexture);
        }


    }
}
