using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;


namespace UnityEngine.Experimental.Rendering.Universal
{
    [BurstCompile]
    internal struct EdgeDetectorJob : IJob
    {
        delegate bool InRange(int valueToCompare);

        public enum TraceError
        {
            NoError,
            ShapeError,
            WindingError
        }

        public enum Exclusivity
        {
            Inclusive,
            Exclusive
        }

        public enum SearchType
        {
            TraceOutline,
            FloodFill
        }


        OutlineSearch m_OutlineSearch;
        bool m_InvertSearch;
        int m_AlphaCutMin;
        int m_AlphaCutMax;
        bool m_InclusiveSearch;
        SearchType m_SearchType;

        NativeArray<int> m_NextShapeIndex;
        NativeArray<int> m_NextContourIndex;
        NativeArray<int2> m_OffsetLookupCW;
        NativeArray<int2> m_OffsetLookupCCW;
        ImageAlpha m_ProcessedAlpha;
        bool m_ExpandEdge;


        public EdgeDetectorJob(ImageAlpha imageAlpha, ref OutlineSearch outlineSearch, int alphaCutMin, int alphaCutMax, bool inclusiveSearch)  // This should take a rect
        {
            m_OutlineSearch = outlineSearch;

            m_InvertSearch = false;
            m_AlphaCutMin = alphaCutMin;
            m_AlphaCutMax = alphaCutMax;
            m_InclusiveSearch = inclusiveSearch;
            m_SearchType = SearchType.TraceOutline;
            m_ProcessedAlpha = imageAlpha;
            m_ExpandEdge = false;

            m_NextShapeIndex = new NativeArray<int>(1, Allocator.Persistent);
            m_NextShapeIndex[0] = 1;

            m_NextContourIndex = new NativeArray<int>(1, Allocator.Persistent);
            m_NextContourIndex[0] = 0;

            m_OffsetLookupCCW = new NativeArray<int2>(8, Allocator.Persistent);
            m_OffsetLookupCCW[0] = new int2(-1, 0);
            m_OffsetLookupCCW[1] = new int2(-1, -1);
            m_OffsetLookupCCW[2] = new int2(0, -1);
            m_OffsetLookupCCW[3] = new int2(1, -1);
            m_OffsetLookupCCW[4] = new int2(1, 0);
            m_OffsetLookupCCW[5] = new int2(1, 1);
            m_OffsetLookupCCW[6] = new int2(0, 1);
            m_OffsetLookupCCW[7] = new int2(-1, 1);

            m_OffsetLookupCW = new NativeArray<int2>(8, Allocator.Persistent);
            m_OffsetLookupCW[0] = new int2(-1, 0);
            m_OffsetLookupCW[1] = new int2(-1, 1);
            m_OffsetLookupCW[2] = new int2(0, 1);
            m_OffsetLookupCW[3] = new int2(1, 1);
            m_OffsetLookupCW[4] = new int2(1, 0);
            m_OffsetLookupCW[5] = new int2(1, -1);
            m_OffsetLookupCW[6] = new int2(0, -1);
            m_OffsetLookupCW[7] = new int2(-1, -1);
        }

        public void UseSearchOptions(Exclusivity exclusivity, SearchType searchType)
        {
            m_InclusiveSearch = exclusivity == Exclusivity.Inclusive;
            m_SearchType = searchType;
        }


        public void SetDetectionRange(int alphaCutMin, int alphaCutMax, bool invert = false)
        {
            m_AlphaCutMin = alphaCutMin;
            m_AlphaCutMax = alphaCutMax;
            m_InvertSearch = invert;
        }

        public void Dispose()
        {
            m_OffsetLookupCCW.Dispose();
            m_OffsetLookupCW.Dispose();
            m_NextContourIndex.Dispose();
            m_NextShapeIndex.Dispose();
        }

        public void Seed()
        {
            m_OutlineSearch.Seed();
        }

        public void SetSearchBuffers(NativeArray<OutlineSearchNode> inputBuffer, NativeArray<int> inputBufferLength, NativeArray<OutlineSearchNode> outputBuffer, NativeArray<int> outputBufferLength)
        {
            m_OutlineSearch.SetSearchBuffers(inputBuffer, inputBufferLength, outputBuffer, outputBufferLength);
        }

        //=============================================================================================================================
        //                                                  Unique functions
        //=============================================================================================================================

        public bool DetectedEdges()
        {
            return m_OutlineSearch.m_OutputBufferLength[0] > 0;
        }

        public void DebugOutput(Texture2D texture, Color color)
        {
            int outputNodeCount = m_OutlineSearch.m_OutputBufferLength[0];

            for (int i = 0; i < outputNodeCount; i++)
            {
                OutlineSearchNode curNode = m_OutlineSearch.m_OutputBuffer[i];
                if (m_OutlineSearch.InsideImageBounds(curNode.x, curNode.y))
                    texture.SetPixel(curNode.x, curNode.y, color);
            }
        }


        public short GetImageAlpha(int x, int y, NativeArray<short> imageAlpha)
        {
            if (m_ProcessedAlpha.InsideImageBounds(x, y))
                return imageAlpha[x + y * m_ProcessedAlpha.width];
            else
                return -1;

        }

        public short GetImageAlpha(int x, int y)
        {
            return GetImageAlpha(x, y, m_ProcessedAlpha.imageAlpha);
        }

        public OutlineTypes.AlphaType GetImageAlphaType(int alpha)
        {
            if (alpha == -1)
                return OutlineTypes.AlphaType.NA;
            else if (alpha < m_AlphaCutMin)
                return OutlineTypes.AlphaType.Transparent;
            else if (alpha >= 255)
                return OutlineTypes.AlphaType.Opaque;
            else
                return OutlineTypes.AlphaType.Translucent;
        }

        public OutlineTypes.AlphaType GetImageAlphaType(int x, int y)
        {
            int alpha = GetImageAlpha(x, y);
            return GetImageAlphaType(alpha);
        }


        public bool AlphaTest(int value)
        {
            return m_InvertSearch ^ (value >= m_AlphaCutMin && value <= m_AlphaCutMax);
        }

        bool IsPartOfOutline(int x, int y, bool isInclusive)
        {
            int value = -1;
            bool retValue = false;
            bool nearValid = true;

            value = GetImageAlpha(x, y);
            retValue = AlphaTest(value);

            if (!isInclusive)
            {
                nearValid = AlphaTest(GetImageAlpha(x - 1, y));
                nearValid |= AlphaTest(GetImageAlpha(x + 1, y));
                nearValid |= AlphaTest(GetImageAlpha(x, y - 1));
                nearValid |= AlphaTest(GetImageAlpha(x, y + 1));
                retValue = nearValid && !retValue;
            }

            return retValue;
        }

        void CreateOutputNode(int x, int y, int shapeIndex, int contourIndex)
        {
            OutlineSearchNode node = new OutlineSearchNode();

            node.x = x;
            node.y = y;
            node.shapeIndex = shapeIndex;
            node.contourIndex = contourIndex;

            m_OutlineSearch.AddOutputNode(ref node);
            m_OutlineSearch.SetClosedCheckNode(ref node);
        }

        bool IsNewMinimum(int2 currentMinimum, int2 testPoint)
        {
            if (testPoint.y < currentMinimum.y)
                return true;
            else if (testPoint.y == currentMinimum.y)
                return testPoint.x < currentMinimum.x;

            return false;
        }

        TraceError TracePath(OutlineSearchNode nodeToProcess, NativeArray<int2> offsetLookupTable, bool isInclusive)
        {
            int savedOutputPosition = m_OutlineSearch.m_OutputBufferLength[0];

            // Add an index for the current path (the current output node)
            int shapeIndex = nodeToProcess.shapeIndex;
            if (shapeIndex <= OutlineConstants.k_UninitializedOutlineId)
                shapeIndex = m_NextShapeIndex[0]++;

            int2 startingPos = new int2(nodeToProcess.x, nodeToProcess.y);
            bool done = false;
            TraceError outlineError = TraceError.NoError;
            int2 currentPos = startingPos;
            int2 minCorner = currentPos;
            int minIndex = m_OutlineSearch.m_OutputBufferLength[0];
            int prevPosAngleOffset = 0;
            int offsetLength = offsetLookupTable.Length;
            int contourIndex = m_NextContourIndex[0];
            int perimeterLength = 1;


            // Find a safe place to start our first winding from. We don't want to start inside a shape.
            bool foundStartPos = false;
            for (int i = 0; !foundStartPos && i < offsetLookupTable.Length; i++)
            {
                int2 testPos = currentPos + offsetLookupTable[i];

                if (!IsPartOfOutline(testPos.x, testPos.y, isInclusive))
                {
                    prevPosAngleOffset = i;
                    foundStartPos = true;
                }
            }


            CreateOutputNode(nodeToProcess.x, nodeToProcess.y, shapeIndex, contourIndex);


            while (!done)
            {
                bool foundNextPos = false;
                // Do a radial check for the next pixel
                for (int i = 0; !foundNextPos && i < offsetLookupTable.Length - 1; i++)
                {
                    int angleOffset = (prevPosAngleOffset + i + 1) % offsetLookupTable.Length; // We don't want to look at the pixel from where we came from, we want to look at the next one.
                    int2 testPos = currentPos + offsetLookupTable[angleOffset];

                    if ((testPos.x != startingPos.x || testPos.y != startingPos.y))
                    {
                        if (IsPartOfOutline(testPos.x, testPos.y, isInclusive))
                        {
                            prevPosAngleOffset = (angleOffset + 4) % offsetLookupTable.Length;
                            currentPos = testPos;
                            foundNextPos = true;

                            if (IsNewMinimum(minCorner, currentPos))
                            {
                                minCorner = testPos;
                                minIndex = m_OutlineSearch.m_OutputBufferLength[0];
                            }

                            CreateOutputNode(testPos.x, testPos.y, shapeIndex, contourIndex);
                            perimeterLength++;
                        }
                    }
                    else
                    {
                        foundNextPos = true;
                        done = true;
                    }
                }

                if (!foundNextPos)
                {
                    outlineError = TraceError.ShapeError;
                    done = true;
                }
            }

            // Check the winding order of the shape
            // Do a radial check for the next pixel
            int secondPosIndex = minIndex + 1;
            if (secondPosIndex >= m_OutlineSearch.m_OutputBufferLength[0])
                secondPosIndex = savedOutputPosition;

            OutlineSearchNode firstNode = m_OutlineSearch.m_OutputBuffer[minIndex];
            OutlineSearchNode secondNode = m_OutlineSearch.m_OutputBuffer[minIndex + 1];
            int2 firstPos = new int2(firstNode.x, firstNode.y);
            int2 secondPos = new int2(secondNode.x, secondNode.y);
            for (int i = 0; outlineError == TraceError.NoError && (i < offsetLookupTable.Length - 1); i++)
            {
                int2 testPos = firstPos + offsetLookupTable[i];
                if (IsPartOfOutline(testPos.x, testPos.y, isInclusive))
                {
                    if (testPos.x != secondPos.x || testPos.y != secondPos.y)
                        outlineError = TraceError.WindingError;

                    break;
                }
            }

            if (outlineError != TraceError.NoError)
                m_OutlineSearch.m_OutputBufferLength[0] = savedOutputPosition;
            else
                m_NextContourIndex[0]++;

            return outlineError;
        }

        public void ExpandEdge(bool expandEdge)
        {
            m_ExpandEdge = expandEdge;
        }


        public bool SearchForContour(OutlineSearchNode nodeToProcess)
        {
            // This function will find pixels inside the valid pixel range to include as the outline.
            bool isPartOfOutline = IsPartOfOutline(nodeToProcess.x, nodeToProcess.y, m_InclusiveSearch);

            if (isPartOfOutline)
            {
                if (m_SearchType == SearchType.TraceOutline)
                {
                    // We should check our first trace path and see if it produces a correct contour
                    TraceError error = TracePath(nodeToProcess, m_OffsetLookupCCW, m_InclusiveSearch);
                    if (error == TraceError.WindingError)
                        error = TracePath(nodeToProcess, m_OffsetLookupCW, m_InclusiveSearch);
                    //Debug.Log("Winding error found.");
                    //    error = TracePath(nodeToProcess, m_OffsetLookupCW, m_InclusiveSearch);


                    // If not, then we try the opposite direction...

                }
            }

            if (!isPartOfOutline)
            {
                m_OutlineSearch.AddInputNode(nodeToProcess.x + 1, nodeToProcess.y, nodeToProcess.shapeIndex);
                m_OutlineSearch.AddInputNode(nodeToProcess.x - 1, nodeToProcess.y, nodeToProcess.shapeIndex);
                m_OutlineSearch.AddInputNode(nodeToProcess.x, nodeToProcess.y + 1, nodeToProcess.shapeIndex);
                m_OutlineSearch.AddInputNode(nodeToProcess.x, nodeToProcess.y - 1, nodeToProcess.shapeIndex);
            }

            return false;
        }

        public void SetStartingShapeIndex(int startingShapeIndex)
        {
            m_NextShapeIndex[0] = startingShapeIndex + 1;
        }

        public int GetFinalShapeIndex()
        {
            return m_NextShapeIndex[0] - 1;
        }

        public void CreateStartPosSortedList(List<Vector2> input, List<Vector2> output)
        {
            Vector2 startingPos = new Vector2(float.MaxValue, float.MaxValue);
            int startingIndex = 0;

            // Find starting pos
            for (int i = 0; i < input.Count; i++)
            {
                if ((input[i].y < startingPos.y) || (input[i].y == startingPos.y && input[i].x < startingPos.x))
                {
                    startingPos = input[i];
                    startingIndex = i;
                }
            }

            // Output
            for (int i = 0; i < input.Count; i++)
            {
                int inputPos = (i + startingIndex) % input.Count;
                output.Add(input[inputPos]);
            }
        }


        public void SaveOutput(OutlineTypes.SaveHandler saveHandler, bool isOuterEdge, OutlineTypes.DebugOutputType debugPixels)
        {
            // We should move part of this into our job

            if (debugPixels == OutlineTypes.DebugOutputType.NA)
            {

                OutlineSearchNode node = m_OutlineSearch.m_OutputBuffer[0];
                int curContourIndex = node.contourIndex;
                int curShapeIndex = node.shapeIndex;

                List<Vector2> outputList = new List<Vector2>();
                List<Vector2> tempOutputList = new List<Vector2>();

                for (int i = 0; i < m_OutlineSearch.m_OutputBufferLength[0]; i++)
                {
                    node = m_OutlineSearch.m_OutputBuffer[i];

                    if (curContourIndex != node.contourIndex || curShapeIndex != node.shapeIndex)
                    {
                        if (tempOutputList.Count > 0)
                        {
                            CreateStartPosSortedList(tempOutputList, outputList);
                            saveHandler(outputList, curShapeIndex - 1, curContourIndex, isOuterEdge);
                        }

                        tempOutputList.Clear();
                        outputList.Clear();
                        curContourIndex = node.contourIndex;
                        curShapeIndex = node.shapeIndex;
                    }

                    tempOutputList.Add(new Vector2(node.x, node.y));
                }

                if (tempOutputList.Count > 0)
                {
                    CreateStartPosSortedList(tempOutputList, outputList);
                    saveHandler(outputList, node.shapeIndex - 1, node.contourIndex, isOuterEdge);
                }
            }
            else if (debugPixels == OutlineTypes.DebugOutputType.Pixels)
            {
                List<Vector2> outputList = new List<Vector2>();
                outputList.Add(Vector2.zero);

                for (int y = 0; y < m_ProcessedAlpha.height; y++)
                {
                    for (int x = 0; x < m_ProcessedAlpha.width; x++)
                    {
                        int alpha = GetImageAlpha(x, y);
                        int alphaType = (int)GetImageAlphaType(alpha);

                        int alphaBounds = (m_AlphaCutMin & 255) | ((m_AlphaCutMax & 255) << 8);

                        outputList[0] = new Vector2(x, y);
                        saveHandler(outputList, alpha, alphaBounds, isOuterEdge);
                    }
                }
            }
        }

        public void Execute()
        {
            if (m_ExpandEdge)
            {
                int nodesToExpand = m_OutlineSearch.m_InputBufferLength[0];
                for (int i = 0; i < nodesToExpand; i++)
                {
                    OutlineSearchNode nodeToProcess = m_OutlineSearch.m_InputBuffer[i];

                    m_OutlineSearch.AddInputNode(nodeToProcess.x + 1, nodeToProcess.y, nodeToProcess.shapeIndex);
                    m_OutlineSearch.AddInputNode(nodeToProcess.x - 1, nodeToProcess.y, nodeToProcess.shapeIndex);
                    m_OutlineSearch.AddInputNode(nodeToProcess.x, nodeToProcess.y + 1, nodeToProcess.shapeIndex);
                    m_OutlineSearch.AddInputNode(nodeToProcess.x, nodeToProcess.y - 1, nodeToProcess.shapeIndex);
                }

                m_ExpandEdge = false;
            }


            m_OutlineSearch.m_OutputBufferLength[0] = 0;
            m_OutlineSearch.m_InputPosition = 0;
            m_OutlineSearch.m_InputLength = m_OutlineSearch.m_InputBufferLength[0];

            // If required do an inclusive search to get inside vertices then do a flip and switch io buffers and search with the given parameters
            while (m_OutlineSearch.HasInput())
            {
                OutlineSearchNode nodeToProcess = new OutlineSearchNode();
                if (m_OutlineSearch.GetNextNode(ref nodeToProcess))
                    SearchForContour(nodeToProcess);
            }

            // Go through the lists and reverse contours that have backward winding
        }
    }
}
