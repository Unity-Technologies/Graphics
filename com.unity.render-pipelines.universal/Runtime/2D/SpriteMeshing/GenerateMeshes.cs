//#define USING_JOBS

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Experimental.Rendering.Universal.LibTessDotNet;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace UnityEngine.Experimental.Rendering.Universal
{
    internal class GenerateMeshes
    {
        static object InterpCustomVertexData(Vec3 position, object[] data, float[] weights)
        {
            return data[0];
        }

        static bool IsReversed(bool isOutsideContour, bool isOpaque)
        {
            return (isOpaque && !isOutsideContour) || (!isOpaque && isOutsideContour);
        }

        static int GetContourHash(List<Vector2> vertices, int vertexCount)
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            MemoryStream memoryStream = new MemoryStream();

            vertices.ForEach((vertex) =>
            {
                binaryFormatter.Serialize(memoryStream, vertex.x);
                binaryFormatter.Serialize(memoryStream, vertex.y);
            });

            int hash = (int)MurmurHash2.Hash(memoryStream.ToArray());
            memoryStream.Dispose();

            return hash;
        }


        static void MakeShapes(ref int startShape, ref int startContour, ShapeLibrary shapeLib, ImageAlpha imageAlpha, int minAlphaCut, bool isOpaque)
        {
            int startingShape = startShape;
            int startingContour = startContour;

            int shapeCount = GenerateOutlines.Generate(imageAlpha, shapeLib.m_Region.width, shapeLib.m_Region.height, minAlphaCut, isOpaque, 0, OutlineTypes.DebugOutputType.NA, (vertices, shapeIndex, contourIndex, isOutsideContour) =>
            {
                shapeIndex = shapeIndex + startingShape;

                if (vertices.Count > 0)
                {
                    int contourId = GetContourHash(vertices, vertices.Count());

                    while (shapeIndex >= shapeLib.m_Shapes.Count)
                        shapeLib.m_Shapes.Add(new Shape());

                    shapeLib.m_Shapes[shapeIndex].m_IsOpaque = isOpaque;
                    Dictionary<int, ContourData> allContourData = shapeLib.m_ContourData;
                    if (!allContourData.ContainsKey(contourId))
                    {
                        ContourData contourData = new ContourData();
                        contourData.m_ContourId = contourId; 
                        contourData.m_UseReverseWinding = IsReversed(isOutsideContour, isOpaque);
                        allContourData.Add(contourId, contourData); // add a new contourData

                        Contour newContour = new Contour(shapeLib.m_Shapes[shapeIndex], contourData, isOutsideContour);
                        shapeLib.m_Shapes[shapeIndex].m_Contours.Add(newContour);
                        contourData.m_Contours.Add(newContour);

                        for (int i = 0; i < vertices.Count; i++)
                            allContourData[contourId].m_Vertices.Add(vertices[i]);
                    }
                    else
                    {
                        ContourData contourData = allContourData[contourId];
                        Contour newContour = new Contour(shapeLib.m_Shapes[shapeIndex], contourData, isOutsideContour);
                        shapeLib.m_Shapes[shapeIndex].m_Contours.Add(newContour);
                        contourData.m_Contours.Add(newContour);
                    }
                }
            }
            );

            startShape += shapeLib.m_Shapes.Count;
            startContour += shapeLib.m_ContourData.Count;
        }

        private static void AddTesselatorContour(Tess tess, RectInt region, Vector2[] contourPath)
        {
            if (contourPath.Length > 0)
            {
                int pointCount = contourPath.Length;
                var inputs = new ContourVertex[pointCount];
                for (int i = 0; i < pointCount; ++i)
                {
                    float u = (float)(contourPath[i].x - region.x) / (float)region.width;
                    float v = (float)(contourPath[i].y - region.y) / (float)region.height;
                    inputs[i] = new ContourVertex() { Position = new Vec3() { X = contourPath[i].x, Y = contourPath[i].y }, Data = new Vector2(u, v) };
                }

                tess.AddContour(inputs, ContourOrientation.CounterClockwise);
            }
        }

        private static void TesselateShape(Tess tess, bool isOpaque, Action<Vector3[], int[], Vector2[], bool> shapeTesselatedHandler)
        {
            tess.Tessellate(WindingRule.EvenOdd, ElementType.Polygons, 3, InterpCustomVertexData);

            var indices = tess.Elements.Select(i => i).ToArray();
            var vertices = tess.Vertices.Select(v => new Vector3(v.Position.X, v.Position.Y, 0)).ToArray();
            var uvs = tess.Vertices.Select(v => new Vector2(((Vector2)v.Data).x, ((Vector2)v.Data).y)).ToArray();

            shapeTesselatedHandler(vertices, indices, uvs, isOpaque);
        }

        public static void TesselateShapes(Vector2[][] customOutline, RectInt region, Action<Vector3[], int[], Vector2[], bool> shapeTesselatedHandler)
        {
            Tess tess = new Tess();

            // Add Custom Outline if one exists
            if (customOutline != null && customOutline.Length > 0)
            {
                foreach (Vector2[] contourPath in customOutline)
                {
                    if (contourPath.Length > 0)
                        AddTesselatorContour(tess, region, contourPath);
                }

                TesselateShape(tess, false, shapeTesselatedHandler);
            }
        }

        public static void TesselateShapes(ShapeLibrary shapeLib, Action<Vector3[], int[], Vector2[], bool> shapeTesselatedHandler)
        {
            foreach (Shape shape in shapeLib.m_Shapes)
            {
                if (shape.m_Contours.Count > 0)
                {
                    Tess tess = new Tess();

                    // Add Contours
                    foreach (Contour contour in shape.m_Contours)
                    {
                        Vector2[] contourPath = contour.m_ContourData.m_Vertices.ToArray();
                        AddTesselatorContour(tess, shapeLib.m_Region, contourPath);

                    }

                    TesselateShape(tess, shape.m_IsOpaque, shapeTesselatedHandler);
                }
            }
        }


        static public void RemoveContour(ShapeLibrary shapeLibrary, Shape shape, int shapeContourIndex)
        {
            int contourId = shape.m_Contours[shapeContourIndex].m_ContourData.m_ContourId;
            shape.m_Contours.RemoveAt(shapeContourIndex);
            shapeLibrary.m_ContourData.Remove(contourId); 
        }

        static public void ReduceShapesAndContours(ShapeLibrary shapeLibrary, float minimumArea)
        {
            for(int shapeIndex= shapeLibrary.m_Shapes.Count-1; shapeIndex >= 0; shapeIndex--)
            {
                Shape shape = shapeLibrary.m_Shapes[shapeIndex];

                if (shape.m_IsOpaque)
                {
                    // For opaque shapes test the outside contour. If smaller, remove the shape from the contours and delete the shape.
                    bool deleteAllContours = false;
                    foreach (Contour contour in shape.m_Contours)
                    {
                        if (contour.m_IsOuterEdge)
                        {
                            float area = contour.CalculateArea();
                            if (area < minimumArea)
                            {
                                deleteAllContours = true;
                                break;
                            }
                        }
                    }

                    if (deleteAllContours)
                    {
                        for (int contourIndex = shape.m_Contours.Count - 1; contourIndex >= 0; contourIndex--)
                        {
                            Contour contour = shape.m_Contours[contourIndex];
                            RemoveContour(shapeLibrary, shape, contourIndex);
                        }
                    }
                }
                else
                {
                    for(int contourIndex = shape.m_Contours.Count - 1; contourIndex >= 0; contourIndex--)
                    {
                        Contour contour = shape.m_Contours[contourIndex];
                        if (!contour.m_IsOuterEdge)
                        {
                            float area = contour.CalculateArea();
                            if (area < minimumArea)
                                RemoveContour(shapeLibrary, shape, contourIndex);
                        }
                    }
                }
            }
        }

        static public void ReduceVertices(ShapeLibrary shapeLibrary, float minimumArea)
        {
            if (shapeLibrary.m_ContourData.Count > 0)
            {
                // Add all our lines to the line intersection manager...
                foreach (KeyValuePair<int, ContourData> dataKV in shapeLibrary.m_ContourData)
                {
                    List<Vector2> vertices = dataKV.Value.m_Vertices;

                    Vector2 prevVertex = vertices[vertices.Count - 1];
                    for (int i = 0; i < vertices.Count; i++)
                    {
                        Vector2 curVertex = vertices[i];
                        shapeLibrary.m_LineIntersectionManager.AddLine(prevVertex, curVertex);
                        prevVertex = curVertex;
                    }
                }

                ReduceShapesAndContours(shapeLibrary, minimumArea);

                // Reduction step
                List<int> contourDataRemovalList = new List<int>();
                foreach (KeyValuePair<int, ContourData> dataKV in shapeLibrary.m_ContourData)
                {
                    float contourArea;
                    VertexReducer vertexReducer = new VertexReducer();
                    vertexReducer.Initialize(shapeLibrary, dataKV.Value.m_Vertices.ToArray(), dataKV.Value.m_UseReverseWinding, out contourArea);

                    vertexReducer.SetConcaveReduction();
                    bool canBeReduced = true;
                    while (vertexReducer.GetSmallestArea() <= minimumArea && canBeReduced)
                        canBeReduced = vertexReducer.ReduceShapeStep();

                    vertexReducer.SetConvexReduction();
                    canBeReduced = true;
                    while (vertexReducer.GetSmallestArea() <= minimumArea && canBeReduced)
                        canBeReduced = vertexReducer.ReduceShapeStep();

                    vertexReducer.GetReducedVertices(out dataKV.Value.m_Vertices);
                }
            }
        }

        static void AddImageBounds(ShapeLibrary shapeLib)
        {
            float2 corner0 = new float2(0, 0);
            float2 corner1 = new float2(shapeLib.m_Region.width, 0);
            float2 corner2 = new float2(shapeLib.m_Region.width, shapeLib.m_Region.height);
            float2 corner3 = new float2(0, shapeLib.m_Region.height);

            shapeLib.m_LineIntersectionManager.AddLine(corner0, corner1);
            shapeLib.m_LineIntersectionManager.AddLine(corner1, corner2);
            shapeLib.m_LineIntersectionManager.AddLine(corner2, corner3);
            shapeLib.m_LineIntersectionManager.AddLine(corner0, corner3);
        }


        static void DebugBoundsImage(ImageAlpha imageAlpha)
        {
            Texture2D texture2D = new Texture2D(imageAlpha.width, imageAlpha.height);

            for (int y = 0; y < imageAlpha.height; y++)
            {
                for (int x = 0; x < imageAlpha.width; x++)
                {
                    int index = y * imageAlpha.width + x;
                    if (imageAlpha.imageAlpha[index] < 0)
                        texture2D.SetPixel(x, y, Color.black);
                    else
                        texture2D.SetPixel(x, y, Color.white);
                }
            }

            byte[] pngData = texture2D.EncodeToPNG();
            System.IO.File.WriteAllBytes("bounds.png", pngData);
        }


        public static void MakeShapes(ShapeLibrary shapeLib, Vector2[][] customOutline, Texture2D texture, short minAlphaCutoff, float minimumArea)
        {
            List<Shape> outlines = new List<Shape>();

            shapeLib.Clear();

            int startingShape = 0;
            int startingContour = 0;

            // Process the image...
            int4 rect = new int4(shapeLib.m_Region.x, shapeLib.m_Region.y, shapeLib.m_Region.width, shapeLib.m_Region.height);
            ImageAlpha processedImageAlpha = new ImageAlpha(rect.z, rect.w);
            ImageAlpha unprocessedImageAlpha = new ImageAlpha(rect.z, rect.w);
            unprocessedImageAlpha.Copy(texture, rect);

            bool hasCustomOutline = customOutline != null && customOutline.Length > 0;
            if (hasCustomOutline)
            { 
                CreateBoundsJob createBoundsJob = new CreateBoundsJob(unprocessedImageAlpha, customOutline);
                #if USING_JOBS
                    createBoundsJob.Run();
                #else
                    createBoundsJob.Execute();
                #endif
                createBoundsJob.Dispose();
            }

            ImageProcessorJob imageProcessorJob = new ImageProcessorJob(minAlphaCutoff, unprocessedImageAlpha, processedImageAlpha);
            #if USING_JOBS
                imageProcessorJob.Run();
            #else
                imageProcessorJob.Execute();
            #endif
            imageProcessorJob.Dispose();

            if(!hasCustomOutline)
                MakeShapes(ref startingShape, ref startingContour, shapeLib, processedImageAlpha, minAlphaCutoff, false);   // Create tranparent geometry
            MakeShapes(ref startingShape, ref startingContour, shapeLib, processedImageAlpha, minAlphaCutoff, true);        // Create opaque geometry

            AddImageBounds(shapeLib);
            ReduceVertices(shapeLib, minimumArea);

            processedImageAlpha.Dispose();
            unprocessedImageAlpha.Dispose();
        }
    }
}
