#define USING_JOBS 

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;

namespace UnityEngine.Experimental.Rendering.Universal
{
    public class GenerateOutlines
    {
        class SearchBuffers
        {
            public NativeArray<OutlineSearchNode> inputBuffer;
            public NativeArray<OutlineSearchNode> outputBuffer;
            public NativeArray<int> inputBufferSize;
            public NativeArray<int> outputBufferSize;

            public SearchBuffers(NativeArray<OutlineSearchNode> input, NativeArray<OutlineSearchNode> output, NativeArray<int> inputSize, NativeArray<int> outputSize)
            {
                inputBuffer = input;
                outputBuffer = output;
                inputBufferSize = inputSize;
                outputBufferSize = outputSize;
            }

            public void InvalidateInputIndices()
            {
                int nodesToExpand = inputBufferSize[0];
                for (int i = 0; i < nodesToExpand; i++)
                {
                    OutlineSearchNode nodeToProcess = inputBuffer[i];
                    nodeToProcess.shapeIndex = OutlineConstants.k_UnknownOutlineId;
                    nodeToProcess.contourIndex = OutlineConstants.k_UnknownOutlineId;
                    inputBuffer[i] = nodeToProcess;
                }
            }

            private static void SwapBuffers<T>(ref NativeArray<T> bufferA, ref NativeArray<T> bufferB) where T : struct
            {
                NativeArray<T> tmp = bufferA;
                bufferA = bufferB;
                bufferB = tmp;
            }


            public void Swap()
            {
                SwapBuffers<OutlineSearchNode>(ref inputBuffer, ref outputBuffer);
                SwapBuffers<int>(ref inputBufferSize, ref outputBufferSize);
            }
        }


        private static bool StartExclusiveSearch(ref EdgeDetectorJob edgeDetectionJob, SearchBuffers searchBuffers, int alphaCutMin, int alphaCutMax, bool invertAlpha, bool expandEdge, bool isOuterEdge, OutlineTypes.SaveHandler saveHandler, OutlineTypes.DebugOutputType debugPixels)
        {
            edgeDetectionJob.UseSearchOptions(EdgeDetectorJob.Exclusivity.Exclusive, EdgeDetectorJob.SearchType.TraceOutline);
            edgeDetectionJob.SetDetectionRange(alphaCutMin, alphaCutMax, invertAlpha);
            edgeDetectionJob.SetSearchBuffers(searchBuffers.inputBuffer, searchBuffers.inputBufferSize, searchBuffers.outputBuffer, searchBuffers.outputBufferSize);
            edgeDetectionJob.ExpandEdge(expandEdge);

#if USING_JOBS
            edgeDetectionJob.Run();
#else
        edgeDetectionJob.Execute();
#endif
            edgeDetectionJob.SaveOutput(saveHandler, isOuterEdge, debugPixels);

            // Find the inclusive edge to start our next search
            searchBuffers.Swap();
            edgeDetectionJob.UseSearchOptions(EdgeDetectorJob.Exclusivity.Inclusive, EdgeDetectorJob.SearchType.FloodFill);
            edgeDetectionJob.SetSearchBuffers(searchBuffers.inputBuffer, searchBuffers.inputBufferSize, searchBuffers.outputBuffer, searchBuffers.outputBufferSize);
            edgeDetectionJob.ExpandEdge(true);

#if USING_JOBS
            edgeDetectionJob.Run();
#else
        edgeDetectionJob.Execute();
#endif
            return edgeDetectionJob.DetectedEdges();
        }


        private static bool StartInclusiveSearch(ref EdgeDetectorJob edgeDetectionJob, SearchBuffers searchBuffers, int alphaCutMin, int alphaCutMax, bool invertAlpha, bool expandEdge, bool isOuterEdge, OutlineTypes.SaveHandler saveHandler, OutlineTypes.DebugOutputType debugPixels)
        {
            edgeDetectionJob.UseSearchOptions(EdgeDetectorJob.Exclusivity.Inclusive, EdgeDetectorJob.SearchType.TraceOutline);
            edgeDetectionJob.SetSearchBuffers(searchBuffers.inputBuffer, searchBuffers.inputBufferSize, searchBuffers.outputBuffer, searchBuffers.outputBufferSize);
            edgeDetectionJob.SetDetectionRange(alphaCutMin, alphaCutMax, invertAlpha);
            edgeDetectionJob.ExpandEdge(expandEdge);

#if USING_JOBS
            edgeDetectionJob.Run();
#else
        edgeDetectionJob.Execute();
#endif

            edgeDetectionJob.SaveOutput(saveHandler, isOuterEdge, debugPixels);

            bool detectedEdges = edgeDetectionJob.DetectedEdges();

            return detectedEdges;
        }

        public static int Generate(ImageAlpha imageAlpha, int width, int height, int alphaCutMin, bool isOpaque, int startingShapeIndex, OutlineTypes.DebugOutputType debugPixels, OutlineTypes.SaveHandler saveHandler)
        {

            // Do edge detection...
            int4 rect = new int4(0, 0, width, height);

            int alphaCutMax = isOpaque ? 255 : 254;
            TileAllocations tile = new TileAllocations(rect, alphaCutMin, 255);

            OutlineSearch outlineSearch = new OutlineSearch();
            outlineSearch.Setup(imageAlpha.width, imageAlpha.height);


            EdgeDetectorJob edgeDetectionJob = new EdgeDetectorJob(imageAlpha, ref outlineSearch, alphaCutMin, alphaCutMax, true); ;
            NativeArray<int> contourIndicesBuffer = new NativeArray<int>(1000, Allocator.Persistent);
            NativeArray<int> contourIndicesBufferLength = new NativeArray<int>(1, Allocator.Persistent);

            edgeDetectionJob.SetSearchBuffers(tile.searchBuffer0, tile.searchBuffer0Size, tile.searchBuffer1, tile.searchBuffer1Size);
            edgeDetectionJob.Seed();
            edgeDetectionJob.SetStartingShapeIndex(startingShapeIndex);

            alphaCutMin = isOpaque ? 255 : alphaCutMin;

            SearchBuffers searchBuffers = new SearchBuffers(tile.searchBuffer0, tile.searchBuffer1, tile.searchBuffer0Size, tile.searchBuffer1Size);

            bool expandEdge = false;
            bool hasNodesToProcess = true;
            while (hasNodesToProcess)
            {
                hasNodesToProcess = StartInclusiveSearch(ref edgeDetectionJob, searchBuffers, alphaCutMin, alphaCutMax, false, expandEdge, true, saveHandler, debugPixels);

                expandEdge = true;
                searchBuffers.Swap();

                hasNodesToProcess = StartInclusiveSearch(ref edgeDetectionJob, searchBuffers, alphaCutMin, alphaCutMax, true, expandEdge, false, saveHandler, debugPixels);

                searchBuffers.Swap();
                searchBuffers.InvalidateInputIndices();
            }

            int finalShapeIndex = edgeDetectionJob.GetFinalShapeIndex();

            tile.Dispose();
            edgeDetectionJob.Dispose();
            contourIndicesBuffer.Dispose();
            contourIndicesBufferLength.Dispose();
            outlineSearch.Dispose();

            return finalShapeIndex;
        }
    }
}
