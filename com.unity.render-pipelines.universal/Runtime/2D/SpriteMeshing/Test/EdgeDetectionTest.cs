using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using UnityEngine.Profiling;
using UnityEditor.Sprites;

namespace UnityEngine.Experimental.Rendering.Universal
{
    [ExecuteInEditMode]
    public class EdgeDetectionTest : MonoBehaviour
    {
        public enum DebugOutput
        {
            DebugEdgesByShape,
            DebugEdgesByContour,
            DebugVertices,
            DebugTransparency,
            DebugSpecklesHoles,
            DebugErrors
        }

        public bool m_Detect = false;
        public DebugOutput m_DebugOutput;
        public bool m_ColorWrap = true;
        public short m_AlphaCutMin = 1;
        public Texture2D m_Input;
        public string m_OutputPath;
        public float m_CrossScale = 10.0f;

        List<Vector3> m_Vertices = new List<Vector3>();

        private void DebugTransparency(List<Vector2> vertex, Texture2D texture, int shapeIndex, int contourIndex)
        {
            int alphaCutMin = contourIndex & 255;
            int alphaCutMax = (contourIndex >> 8) & 255;

            if (shapeIndex <= alphaCutMax && shapeIndex >= alphaCutMin)
            {
                Color color = Color.red;
                if (shapeIndex < 255)
                    color = Color.green;

                texture.SetPixel((int)vertex[0].x + 1, (int)vertex[0].y + 1, color);
            }
        }

        private void DebugSpecklesHoles(List<Vector2> vertex, Texture2D texture, int shapeIndex, int contourIndex)
        {
            int alphaCutMin = contourIndex & 255;
            int alphaCutMax = (contourIndex >> 8) & 255;


            if (shapeIndex == 254)
                texture.SetPixel((int)vertex[0].x + 1, (int)vertex[0].y + 1, Color.red);
            else if (shapeIndex == 1)
                texture.SetPixel((int)vertex[0].x + 1, (int)vertex[0].y + 1, Color.green);
        }


        private void DebugEdges(List<Vector2> vertices, Texture2D texture, int shapeIndex, int contourIndex)
        {
            //DebugTransparency(vertex, texture, index);
            Color[] colors = new Color[11]
            {
            Color.black,
            Color.red,
            Color.green,
            Color.blue,
            Color.yellow,
            Color.magenta,
            Color.cyan,
            new Color(0.4f, 0.0f, 0.0f),
            new Color(0.0f, 0.4f, 0.0f),
            new Color(0.0f, 0.0f, 0.4f),
            Color.gray,
            };


            shapeIndex = shapeIndex & 0xFFFFFF;

            int index = m_DebugOutput == DebugOutput.DebugEdgesByShape ? shapeIndex : contourIndex;

            if (index < 0)
                index = 0;

            if (!m_ColorWrap && index >= colors.Length)
                index = colors.Length - 1;

            for (int i = 0; i < vertices.Count; i++)
                texture.SetPixel((int)vertices[i].x + 1, (int)vertices[i].y + 1, colors[index % colors.Length]);
        }


        int m_DebugVertexColor = 0;
        private void DebugVertices(List<Vector2> vertices, Texture2D texture, int shapeIndex, int contourIndex)
        {
            for (int i = 0; i < vertices.Count; i++)
            {
                int shade = (m_DebugVertexColor >> 8) % 3;
                float intensity = (float)(m_DebugVertexColor % 256) / 255.0f;

                Color color = new Color(intensity, intensity, intensity, 1.0f);

                if (shade == 0)
                    color = intensity * Color.red + (1 - intensity) * Color.blue;
                else if (shade == 1)
                    color = intensity * Color.green + (1 - intensity) * Color.red;
                else if (shade == 2)
                    color = intensity * Color.blue + (1 - intensity) * Color.green;


                texture.SetPixel((int)vertices[i].x + 1, (int)vertices[i].y + 1, color);
                m_DebugVertexColor++;
            }
        }



        void DrawCross(Texture2D texture, int2 point, float size, Color color)
        {
            texture.SetPixel(point.x, point.y, color);

            for (int i = 0; i < size; i++)
            {
                texture.SetPixel(point.x - i, point.y, color);
                texture.SetPixel(point.x + i, point.y, color);
                texture.SetPixel(point.x, point.y - i, color);
                texture.SetPixel(point.x, point.y + i, color);
            }
        }


        // Update is called once per frame
        void Update()
        {
            Action<List<Vector2>, Texture2D, int, int> debugHandler = DebugEdges;
            string filename = "debugshapes";
            OutlineTypes.DebugOutputType debugType = OutlineTypes.DebugOutputType.NA;

            if (m_DebugOutput == DebugOutput.DebugVertices)
            {
                debugHandler = DebugVertices;
                filename = "debugvertices";
            }
            else if (m_DebugOutput == DebugOutput.DebugEdgesByShape)
            {
                filename = "debugshapes";
            }
            else if (m_DebugOutput == DebugOutput.DebugEdgesByContour)
            {
                filename = "debugcontours";
            }
            else if (m_DebugOutput == DebugOutput.DebugTransparency)
            {
                debugHandler = DebugTransparency;
                filename = "debugtransparency";
                debugType = OutlineTypes.DebugOutputType.Pixels;
            }
            else if (m_DebugOutput == DebugOutput.DebugSpecklesHoles)
            {
                debugHandler = DebugSpecklesHoles;
                filename = "debugspeckles";
                debugType = OutlineTypes.DebugOutputType.SpeckleHoleMap;
            }
            else if (m_DebugOutput == DebugOutput.DebugErrors)
            {
                debugHandler = DebugEdges;
                filename = "debugerrors";
                debugType = OutlineTypes.DebugOutputType.Errors;
            }

            if (m_Detect)
            {
                m_Detect = false;

                int width = m_Input.width;
                int height = m_Input.height;


                System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
                Texture2D debugTransparent = new Texture2D(width + 2, height + 2);
                debugTransparent.filterMode = FilterMode.Point;
                Texture2D debugOpaque = new Texture2D(width + 2, height + 2);
                debugTransparent.filterMode = FilterMode.Point;

                int4 rect = new int4(0, 0, m_Input.width, m_Input.height);
                ImageAlpha processedImageAlpha = new ImageAlpha(m_Input.width, m_Input.height);
                ImageAlpha unprocessedImageAlpha = new ImageAlpha(m_Input.width, m_Input.height);
                unprocessedImageAlpha.Copy(m_Input, rect);

                ImageProcessorJob imageProcessorJob = new ImageProcessorJob(m_AlphaCutMin, unprocessedImageAlpha, processedImageAlpha);

#if USING_JOBS
        imageProcessorJob.Run();
#else
                imageProcessorJob.Execute();
#endif
                imageProcessorJob.Dispose();

                m_Vertices.Clear();
                int shapeCount = GenerateOutlines.Generate(processedImageAlpha, width, height, m_AlphaCutMin, false, 0, debugType, (vertex, shapeIndex, contourIndex, isOuterEdge) => { debugHandler(vertex, debugTransparent, shapeIndex, contourIndex); });
                m_Vertices.Clear();
                shapeCount = GenerateOutlines.Generate(processedImageAlpha, width, height, m_AlphaCutMin, true, shapeCount, debugType, (vertex, shapeIndex, contourIndex, isOuterEdge) => { debugHandler(vertex, debugOpaque, shapeIndex, contourIndex); });

                stopwatch.Stop();
                Debug.Log("Time: " + (float)stopwatch.ElapsedMilliseconds / 1000.0f);
                Debug.Log("Shape Count = " + shapeCount);

                byte[] data = debugTransparent.EncodeToPNG();
                File.WriteAllBytes(m_OutputPath.ToLower() + filename + "_transparent.png", data);

                data = debugOpaque.EncodeToPNG();
                File.WriteAllBytes(m_OutputPath.ToLower() + filename + "_opaque.png", data);

                Texture2D debugTex = new Texture2D(width + 2, height + 2);
                debugTex.filterMode = FilterMode.Point;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Color color = m_Input.GetPixel(x, y);
                        debugTex.SetPixel(x + 1, y + 1, color);
                    }
                }

                for (int y = 0; y < height + 2; y++)
                {
                    debugTex.SetPixel(0, y, Color.black);
                    debugTex.SetPixel(width + 1, y, Color.black);
                }

                for (int x = 0; x < width + 2; x++)
                {
                    debugTex.SetPixel(x, 0, Color.black);
                    debugTex.SetPixel(x, height + 1, Color.black);
                }



                data = debugTex.EncodeToPNG();
                File.WriteAllBytes(m_OutputPath.ToLower() + filename + "_debug.png", data);

                processedImageAlpha.Dispose();
                unprocessedImageAlpha.Dispose();
            }
        }
    }
}
