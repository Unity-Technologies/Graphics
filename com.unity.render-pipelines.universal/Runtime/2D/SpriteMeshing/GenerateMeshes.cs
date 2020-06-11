//#define USING_JOBS

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Experimental.Rendering.Universal.LibTessDotNet;

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

        static int GetContourHash(Vector2 firstVertex, int vertexCount)
        {
            return ((int)firstVertex.x & 8191) | (((int)firstVertex.y & 8191) << 13) | ((vertexCount & 63) << 26);
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
                    int contourId = GetContourHash(vertices[0], vertices.Count());

                    while (shapeIndex >= shapeLib.m_Shapes.Count)
                        shapeLib.m_Shapes.Add(new Shape());

                    shapeLib.m_Shapes[shapeIndex].m_IsOpaque = isOpaque;
                    Dictionary<int, ContourData> allContourData = shapeLib.m_ContourData;
                    if (!allContourData.ContainsKey(contourId))
                    {
                        ContourData contourData = new ContourData();
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

        public static void TesselateShapes(ShapeLibrary shapeLib, Action<Vector3[], int[], Vector2[], bool> shapeTesselatedHandler)
        {
            foreach (Shape shape in shapeLib.m_Shapes)
            {
                if (shape.m_Contours.Count > 0)
                {
                    RectInt region = shapeLib.m_Region;

                    Tess tessI = new Tess();

                    // Add Contours
                    foreach (Contour contour in shape.m_Contours)
                    {
                        List<Vector2> shapePath = contour.m_ContourData.m_Vertices;
                        if (shapePath.Count > 0)
                        {
                            int pointCount = shapePath.Count;
                            var inputs = new ContourVertex[pointCount];
                            for (int i = 0; i < pointCount; ++i)
                            {
                                float u = (float)(shapePath[i].x - region.x) / (float)region.width;
                                float v = (float)(shapePath[i].y - region.y) / (float)region.height;
                                inputs[i] = new ContourVertex() { Position = new Vec3() { X = shapePath[i].x, Y = shapePath[i].y }, Data = new Vector2(u, v) };
                            }

                            tessI.AddContour(inputs, ContourOrientation.CounterClockwise);
                        }
                    }

                    tessI.Tessellate(WindingRule.EvenOdd, ElementType.Polygons, 3, InterpCustomVertexData);

                    var indicesI = tessI.Elements.Select(i => i).ToArray();
                    var verticesI = tessI.Vertices.Select(v => new Vector3(v.Position.X, v.Position.Y, 0)).ToArray();
                    var uvsI = tessI.Vertices.Select(v => new Vector2(((Vector2)v.Data).x, ((Vector2)v.Data).y)).ToArray();

                    List<Vector3> finalVertices = new List<Vector3>();
                    List<int> finalIndices = new List<int>();
                    List<Vector2> finalUVs = new List<Vector2>();

                    finalVertices.AddRange(verticesI);
                    finalIndices.AddRange(indicesI);
                    finalUVs.AddRange(uvsI);

                    shapeTesselatedHandler(finalVertices.ToArray(), finalIndices.ToArray(), finalUVs.ToArray(), shape.m_IsOpaque);
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

                // Reduction step
                List<int> contourDataRemovalList = new List<int>();
                foreach (KeyValuePair<int, ContourData> dataKV in shapeLibrary.m_ContourData)
                {
                    float contourArea;
                    VertexReducer vertexReducer = new VertexReducer();
                    vertexReducer.Initialize(shapeLibrary, dataKV.Value.m_Vertices.ToArray(), dataKV.Value.m_UseReverseWinding, out contourArea);

                    if (contourArea < minimumArea)
                    {
                        if (dataKV.Value.m_Contours.Count > 1)
                            contourDataRemovalList.Add(dataKV.Key);
                    }
                    else
                    {
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

                while (contourDataRemovalList.Count > 0)
                {
                    int dataToRemove = contourDataRemovalList[0];
                    contourDataRemovalList.RemoveAt(0);

                    ContourData contourData = shapeLibrary.m_ContourData[dataToRemove];
                    for (int i = 0; i < contourData.m_Contours.Count; i++)
                    {
                        Contour contour = contourData.m_Contours[i];
                        contour.m_Shape.m_Contours.Remove(contour);
                    }

                    shapeLibrary.m_ContourData.Remove(dataToRemove);
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

        static void AddImageBounds(ShapeLibrary shapeLib, List<Vector2[]> outlines)
        {
            for (int outlineIndex = 0; outlineIndex < outlines.Count; outlineIndex++)
            {
                Vector2[] outline = outlines[outlineIndex];

                Vector2 preVertex = outline[outline.Length - 1];
                for (int outlineElement = 0; outlineElement < outline.Length; outlineElement++)
                {
                    Vector2 curVertex = outline[outlineElement];
                    shapeLib.m_LineIntersectionManager.AddLine(preVertex, curVertex);
                    preVertex = curVertex;
                }
            }
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


        public static void MakeShapes(ShapeLibrary shapeLib, Texture2D texture, short minAlphaCutoff, float minimumArea)
        {
            List<Shape> outlines = new List<Shape>();

            shapeLib.Clear();

            int startingShape = 0;
            int startingContour = 0;

            // Process the image...
            int4 rect = new int4(shapeLib.m_Region.x, shapeLib.m_Region.y, shapeLib.m_Region.width, shapeLib.m_Region.height);
            ImageAlpha processedImageAlpha = new ImageAlpha(texture.width, texture.height);
            ImageAlpha unprocessedImageAlpha = new ImageAlpha(texture.width, texture.height);
            unprocessedImageAlpha.Copy(texture, rect);


            //float size = 2000;
            List<Vector2[]> boundsOutlines = new List<Vector2[]>();
            //Vector2[] boundsOutline0 = new Vector2[4]
            //{
            //    new Vector2(0,0),
            //    new Vector2(texture.width-1, 0),
            //    new Vector2(texture.width-1, 750),
            //    new Vector2(0, 750)
            //};

            //Vector2[] boundsOutline1 = new Vector2[3]
            //{
            //    new Vector2(0,1000),
            //    new Vector2(0.5f * texture.width, texture.height-1),
            //    new Vector2(texture.width-1, 1000)
            //};


            //boundsOutlines.Add(boundsOutline0);
            //boundsOutlines.Add(boundsOutline1);

            //foreach (Vector2[] boundsOutline in boundsOutlines)
            //{
            //    int prevIndex = boundsOutline.Length - 1;
            //    for (int i = 0; i < boundsOutline.Length; i++)
            //    {
            //        Debug.DrawLine(boundsOutline[prevIndex], boundsOutline[i], Color.yellow, 20);
            //        prevIndex = i;
            //    }
            //}


            CreateBoundsJob createBoundsJob = new CreateBoundsJob(unprocessedImageAlpha, boundsOutlines);
#if USING_JOBS
        createBoundsJob.Run();
#else
            createBoundsJob.Execute();
#endif
            createBoundsJob.Dispose();


            //DebugBoundsImage(unprocessedImageAlpha);
            //return;

            ImageProcessorJob imageProcessorJob = new ImageProcessorJob(minAlphaCutoff, unprocessedImageAlpha, processedImageAlpha);
#if USING_JOBS
        imageProcessorJob.Run();
#else
            imageProcessorJob.Execute();
#endif
            imageProcessorJob.Dispose();

            GenerateMeshes.MakeShapes(ref startingShape, ref startingContour, shapeLib, processedImageAlpha, minAlphaCutoff, false);
            GenerateMeshes.MakeShapes(ref startingShape, ref startingContour, shapeLib, processedImageAlpha, minAlphaCutoff, true);


            //AddImageBounds(shapeLib, boundsOutlines);
            AddImageBounds(shapeLib);
            ReduceVertices(shapeLib, minimumArea);

            processedImageAlpha.Dispose();
            unprocessedImageAlpha.Dispose();
        }
    }
}
