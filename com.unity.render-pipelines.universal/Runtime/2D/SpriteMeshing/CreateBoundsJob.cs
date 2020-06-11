using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;

namespace UnityEngine.Experimental.Rendering.Universal
{
    [BurstCompile]
    internal struct CreateBoundsJob : IJob
    {
        const int k_HasBeenWritten = -1;
        const int k_HasBeenQueued = -2;

        NativeArray<float2> m_Outlines;
        NativeArray<byte> m_LineMask;
        NativeArray<int> m_EndIndices;
        ImageAlpha m_ImageAlpha;

        public CreateBoundsJob(ImageAlpha imageAlpha, List<Vector2[]> outlines)
        {
            m_ImageAlpha = imageAlpha;
            m_LineMask = new NativeArray<byte>(imageAlpha.width * imageAlpha.height, Allocator.Persistent, NativeArrayOptions.ClearMemory);

            // Count the elements used
            int totalOutlineElements = 0;
            for (int outlineIdx = 0; outlineIdx < outlines.Count; outlineIdx++)
            {
                Vector2[] outline = outlines[outlineIdx];
                for (int outlineElementIdx = 0; outlineElementIdx < outline.Length; outlineElementIdx++)
                    totalOutlineElements++;
            }

            m_Outlines = new NativeArray<float2>(totalOutlineElements, Allocator.Persistent);
            m_EndIndices = new NativeArray<int>(outlines.Count, Allocator.Persistent);

            int elementsCopied = 0;
            for (int outlineIdx = 0; outlineIdx < outlines.Count; outlineIdx++)
            {
                Vector2[] outline = outlines[outlineIdx];
                for (int outlineElementIdx = 0; outlineElementIdx < outline.Length; outlineElementIdx++)
                    m_Outlines[elementsCopied++] = outline[outlineElementIdx];

                m_EndIndices[outlineIdx] = elementsCopied - 1;
            }
        }

        public void Dispose()
        {
            m_Outlines.Dispose();
            m_EndIndices.Dispose();
            m_LineMask.Dispose();
        }

        bool InsideImageBounds(int x, int y)
        {
            return (x >= 0) && (y >= 0) && (x < m_ImageAlpha.width) && (y < m_ImageAlpha.height);
        }

        bool HasBeenWritten(int2 point)
        {
            if (InsideImageBounds(point.x, point.y))
            {
                int index = point.y * m_ImageAlpha.width + point.x;
                return m_ImageAlpha.imageAlpha[index] == k_HasBeenWritten;
            }
            return true;
        }

        bool IsBoundry(int2 point)
        {
            if (InsideImageBounds(point.x, point.y))
            {
                int index = point.y * m_ImageAlpha.width + point.x;
                return m_LineMask[index] != 0;
            }
            return true;
        }

        bool HasBeenVisited(int2 point)
        {
            if (InsideImageBounds(point.x, point.y))
            {
                int index = point.y * m_ImageAlpha.width + point.x;
                return m_ImageAlpha.imageAlpha[index] < 0;
            }
            return true;
        }

        void MarkAsQueued(int2 point)
        {
            int index = point.y * m_ImageAlpha.width + point.x;
            m_ImageAlpha.imageAlpha[index] = k_HasBeenQueued;
        }

        public void Execute()
        {
            if (m_EndIndices.Length == 0)
                return;


            IterativeLine iterLine = new IterativeLine();

            // Add line mask
            int newLineEndIndex = 0;
            int startIndex = m_EndIndices[newLineEndIndex];
            for (int i = 0; i < m_Outlines.Length; i++)
            {
                int2 start = new int2((int)m_Outlines[startIndex].x, (int)m_Outlines[startIndex].y);
                int2 end = new int2((int)m_Outlines[i].x, (int)m_Outlines[i].y);

                iterLine.Initialize(start, end);
                while (!iterLine.IsEnd())
                {
                    int2 value = iterLine.Current();
                    int maskIndex = m_ImageAlpha.width * value.y + value.x;
                    m_LineMask[maskIndex] = 1;  // This needs to be non-zero
                    iterLine.Step();
                }

                if ((newLineEndIndex + 1) < m_EndIndices.Length && i == m_EndIndices[newLineEndIndex])
                {
                    newLineEndIndex++;
                    startIndex = m_EndIndices[newLineEndIndex];
                }
                else
                {
                    startIndex = i;
                }
            }

            // Do search with line mask
            int searchQueueEnd = 0;
            NativeArray<int2> searchQueue = new NativeArray<int2>(m_ImageAlpha.width * m_ImageAlpha.height, Allocator.Temp);

            for (int x = 1; x < m_ImageAlpha.width - 1; x++)
            {
                searchQueue[searchQueueEnd] = new int2(x, 0);
                if (!IsBoundry(searchQueue[searchQueueEnd])) MarkAsQueued(searchQueue[searchQueueEnd++]);
                searchQueue[searchQueueEnd] = new int2(x, m_ImageAlpha.height - 1);
                if (!IsBoundry(searchQueue[searchQueueEnd])) MarkAsQueued(searchQueue[searchQueueEnd++]);
            }

            for (int y = 0; y < m_ImageAlpha.height; y++)
            {
                searchQueue[searchQueueEnd] = new int2(0, y);
                if (!IsBoundry(searchQueue[searchQueueEnd])) MarkAsQueued(searchQueue[searchQueueEnd++]);
                searchQueue[searchQueueEnd] = new int2(m_ImageAlpha.width - 1, y);
                if (!IsBoundry(searchQueue[searchQueueEnd])) MarkAsQueued(searchQueue[searchQueueEnd++]);
            }

            while (searchQueueEnd > 0)
            {
                int2 positionToProcess = searchQueue[--searchQueueEnd];

                int index = m_ImageAlpha.width * positionToProcess.y + positionToProcess.x;

                if (!HasBeenWritten(positionToProcess) && !IsBoundry(positionToProcess))
                {
                    m_ImageAlpha.imageAlpha[index] = k_HasBeenWritten;

                    int2 up = new int2(positionToProcess.x, positionToProcess.y + 1);
                    int2 down = new int2(positionToProcess.x, positionToProcess.y - 1);
                    int2 left = new int2(positionToProcess.x + 1, positionToProcess.y);
                    int2 right = new int2(positionToProcess.x - 1, positionToProcess.y);

                    if (!HasBeenVisited(up) && !HasBeenWritten(up) && !IsBoundry(up))
                    {
                        searchQueue[searchQueueEnd++] = up;
                        MarkAsQueued(up);
                    }
                    if (!HasBeenVisited(down) && !HasBeenWritten(down) && !IsBoundry(down))
                    {
                        searchQueue[searchQueueEnd++] = down;
                        MarkAsQueued(down);
                    }
                    if (!HasBeenVisited(left) && !HasBeenWritten(left) && !IsBoundry(left))
                    {
                        searchQueue[searchQueueEnd++] = left;
                        MarkAsQueued(left);
                    }
                    if (!HasBeenVisited(right) && !HasBeenWritten(right) && !IsBoundry(right))
                    {
                        searchQueue[searchQueueEnd++] = right;
                        MarkAsQueued(right);
                    }
                }
            }

            searchQueue.Dispose();
        }
    }
}
