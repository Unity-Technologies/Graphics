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
    internal struct ImageProcessorJob : IJob
    {
        const int k_HoleSearchBailout = 5000;
        ImageAlpha m_UnprocessedImageAlpha;
        ImageAlpha m_ProcessedImageAlpha;
        short m_AlphaCutoff;

        public ImageProcessorJob(short minAlphaCutoff, ImageAlpha inImageAlpha, ImageAlpha outImageAlpha)
        {
            m_UnprocessedImageAlpha = inImageAlpha;
            m_ProcessedImageAlpha = outImageAlpha;


            m_AlphaCutoff = minAlphaCutoff;
        }

        public void Dispose()
        {
        }

        OutlineTypes.AlphaType GetImageAlphaType(ref ImageAlpha imageAlpha, int x, int y)
        {
            return imageAlpha.GetImageAlphaType(imageAlpha.GetImageAlpha(x, y), m_AlphaCutoff);
        }

        bool IsSpeckle(int2 position)
        {
            if (m_ProcessedImageAlpha.InsideImageBounds(position.x, position.y))
            {
                if (GetImageAlphaType(ref m_ProcessedImageAlpha, position.x, position.y) == OutlineTypes.AlphaType.Translucent)
                {
                    int numberOfTranslucentNeighbors = 0;
                    numberOfTranslucentNeighbors += GetImageAlphaType(ref m_ProcessedImageAlpha, position.x + 1, position.y) == OutlineTypes.AlphaType.Transparent ? 1 : 0;
                    numberOfTranslucentNeighbors += GetImageAlphaType(ref m_ProcessedImageAlpha, position.x - 1, position.y) == OutlineTypes.AlphaType.Transparent ? 1 : 0;
                    numberOfTranslucentNeighbors += GetImageAlphaType(ref m_ProcessedImageAlpha, position.x, position.y + 1) == OutlineTypes.AlphaType.Transparent ? 1 : 0;
                    numberOfTranslucentNeighbors += GetImageAlphaType(ref m_ProcessedImageAlpha, position.x, position.y - 1) == OutlineTypes.AlphaType.Transparent ? 1 : 0;

                    return numberOfTranslucentNeighbors >= 4;
                }
            }
            return false;
        }

        bool IsHole(int2 position)
        {
            if (m_ProcessedImageAlpha.InsideImageBounds(position.x, position.y))
            {
                if (GetImageAlphaType(ref m_ProcessedImageAlpha, position.x, position.y) != OutlineTypes.AlphaType.Translucent)
                {
                    int numberOfTranslucentNeighbors = 0;

                    numberOfTranslucentNeighbors += GetImageAlphaType(ref m_ProcessedImageAlpha, position.x + 1, position.y) == OutlineTypes.AlphaType.Translucent ? 1 : 0;
                    numberOfTranslucentNeighbors += GetImageAlphaType(ref m_ProcessedImageAlpha, position.x - 1, position.y) == OutlineTypes.AlphaType.Translucent ? 1 : 0;
                    numberOfTranslucentNeighbors += GetImageAlphaType(ref m_ProcessedImageAlpha, position.x, position.y + 1) == OutlineTypes.AlphaType.Translucent ? 1 : 0;
                    numberOfTranslucentNeighbors += GetImageAlphaType(ref m_ProcessedImageAlpha, position.x, position.y - 1) == OutlineTypes.AlphaType.Translucent ? 1 : 0;

                    return numberOfTranslucentNeighbors >= 3;
                }
            }
            return false;
        }

        bool IsNarrow(int2 position)
        {
            if (GetImageAlphaType(ref m_ProcessedImageAlpha, position.x, position.y) != OutlineTypes.AlphaType.Translucent)
            {
                int leftRightNeighbors = 0;
                int upDownNeighbors = 0;

                leftRightNeighbors += GetImageAlphaType(ref m_ProcessedImageAlpha, position.x + 1, position.y) == OutlineTypes.AlphaType.Translucent ? 1 : 0;
                leftRightNeighbors += GetImageAlphaType(ref m_ProcessedImageAlpha, position.x - 1, position.y) == OutlineTypes.AlphaType.Translucent ? 1 : 0;
                upDownNeighbors += GetImageAlphaType(ref m_ProcessedImageAlpha, position.x, position.y + 1) == OutlineTypes.AlphaType.Translucent ? 1 : 0;
                upDownNeighbors += GetImageAlphaType(ref m_ProcessedImageAlpha, position.x, position.y - 1) == OutlineTypes.AlphaType.Translucent ? 1 : 0;

                return upDownNeighbors == 2 || leftRightNeighbors == 2;
            }

            return false;
        }

        void ProcessImage()
        {
            NativeArray<int2> pixelOffset = new NativeArray<int2>(8, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            pixelOffset[0] = new int2(0, -1);
            pixelOffset[1] = new int2(0, 1);
            pixelOffset[2] = new int2(-1, 0);
            pixelOffset[3] = new int2(1, 0);
            pixelOffset[4] = new int2(1, -1);
            pixelOffset[5] = new int2(-1, 1);
            pixelOffset[6] = new int2(1, 1);
            pixelOffset[7] = new int2(-1, -1);


            // This should really be its own job probably
            for (int y = 0; y < m_UnprocessedImageAlpha.height; y++)
            {
                for (int x = 0; x < m_UnprocessedImageAlpha.width; x++)
                {
                    int index = y * m_UnprocessedImageAlpha.width + x;
                    short srcAlphaValue = m_UnprocessedImageAlpha.imageAlpha[index];
                    short destAlphaValue = m_ProcessedImageAlpha.imageAlpha[index];
                    short alphaValue = srcAlphaValue > destAlphaValue ? srcAlphaValue : destAlphaValue;
                    short adjAlphaValue = alphaValue == 255 ? (byte)254 : alphaValue;

                    if (srcAlphaValue >= m_AlphaCutoff)
                    {
                        bool hasAdjacentTransparency = false;

                        for (int i = 0; i < pixelOffset.Length; i++)
                        {
                            int2 offset = pixelOffset[i];
                            int2 offsetPos = new int2(x + offset.x, y + offset.y);
                            int offsetIndex = offsetPos.x + offsetPos.y * m_UnprocessedImageAlpha.width;

                            if (m_UnprocessedImageAlpha.InsideImageBounds(offsetPos.x, offsetPos.y))
                            {
                                short srcOffsetAlpha = m_UnprocessedImageAlpha.imageAlpha[offsetIndex];
                                short destOffsetAlpha = m_ProcessedImageAlpha.imageAlpha[offsetIndex];

                                if (srcOffsetAlpha != 255)
                                {
                                    m_ProcessedImageAlpha.imageAlpha[offsetIndex] = adjAlphaValue > destOffsetAlpha ? adjAlphaValue : destOffsetAlpha;
                                    hasAdjacentTransparency = true;
                                }
                            }
                        }

                        if (alphaValue == 255 && hasAdjacentTransparency)
                            m_ProcessedImageAlpha.imageAlpha[index] = 254;
                        else
                            m_ProcessedImageAlpha.imageAlpha[index] = alphaValue;
                    }
                }
            }

            // Despeckle/Remove holes in alpha and opaque.  
            int maxHoleSearchStackSize = m_UnprocessedImageAlpha.width * m_UnprocessedImageAlpha.height;
            int holeSearchStackLength = 0;
            NativeArray<int2> holeSearchStack = new NativeArray<int2>(maxHoleSearchStackSize, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            for (int y = 0; y < m_UnprocessedImageAlpha.height; y++)
            {
                for (int x = 0; x < m_UnprocessedImageAlpha.width; x++)
                {
                    int2 currentPos = new int2(x, y);
                    if (IsHole(currentPos))
                    {
                        holeSearchStack[holeSearchStackLength++] = currentPos;
                        while (holeSearchStackLength > 0 && holeSearchStackLength < k_HoleSearchBailout)
                        {
                            int2 poppedPos = holeSearchStack[--holeSearchStackLength];
                            int currentIndex = poppedPos.x + poppedPos.y * m_UnprocessedImageAlpha.width;

                            if (IsHole(poppedPos))
                            {
                                m_ProcessedImageAlpha.imageAlpha[currentIndex] = 254;

                                holeSearchStack[holeSearchStackLength++] = new int2(poppedPos.x + 1, poppedPos.y);
                                holeSearchStack[holeSearchStackLength++] = new int2(poppedPos.x - 1, poppedPos.y);
                                holeSearchStack[holeSearchStackLength++] = new int2(poppedPos.x, poppedPos.y + 1);
                                holeSearchStack[holeSearchStackLength++] = new int2(poppedPos.x, poppedPos.y - 1);
                            }
                        }
                    }
                }
            }

            // Remove narrow parts
            for (int y = 0; y < m_UnprocessedImageAlpha.height; y++)
            {
                for (int x = 0; x < m_UnprocessedImageAlpha.width; x++)
                {
                    int2 currentPos = new int2(x, y);
                    int currentIndex = currentPos.x + currentPos.y * m_UnprocessedImageAlpha.width;
                    if (IsNarrow(currentPos))
                    {
                        m_ProcessedImageAlpha.imageAlpha[currentIndex] = 254;
                    }
                    else if (IsSpeckle(currentPos))
                    {
                        m_ProcessedImageAlpha.imageAlpha[currentIndex] = 0;
                    }
                }
            }

            pixelOffset.Dispose();
        }

        public void Execute()
        {
            ProcessImage();
        }
    }

}
