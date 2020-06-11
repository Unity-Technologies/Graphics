using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;

namespace UnityEngine.Experimental.Rendering.Universal
{
    internal struct OutlineSearch
    {
        public int m_Width;
        public int m_Height;

        public int m_InputPosition;
        public int m_InputLength;

        // To open a node, add it to m_NodesToProcess and m_OpenCheckNodes. When closing a node remove it from m_NodesToProcess, and m_OpenCheckNodes then add it to m_ClosedCheckNodes
        public NativeArray<int> m_InputBufferLength;
        public NativeArray<int> m_OutputBufferLength;

        public NativeArray<OutlineSearchNode> m_InputBuffer;
        public NativeArray<OutlineSearchNode> m_OutputBuffer;

        public NativeArray<int> m_OpenCheckNodes;
        public NativeArray<int> m_ClosedCheckNodes;

        //========================================================================================================================
        //                                              Public - Setup/Teardown
        //========================================================================================================================
        public void Setup(int width, int height)  // This should take a rect
        {
            m_Width = width;
            m_Height = height;
            int arraySize = (width + 2 * OutlineConstants.k_BorderSize) * (height + 2 * OutlineConstants.k_BorderSize);
            m_OpenCheckNodes = new NativeArray<int>(arraySize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            m_ClosedCheckNodes = new NativeArray<int>(arraySize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        }

        public void Dispose()
        {
            m_OpenCheckNodes.Dispose();
            m_ClosedCheckNodes.Dispose();
        }

        public void Seed()
        {
            AddInputNode(-1, -1, OutlineConstants.k_UnknownOutlineId);
        }

        public void SetSearchBuffers(NativeArray<OutlineSearchNode> inputBuffer, NativeArray<int> inputBufferLength, NativeArray<OutlineSearchNode> outputBuffer, NativeArray<int> outputBufferLength)
        {
            m_InputPosition = 0;
            m_InputBuffer = inputBuffer;
            m_OutputBuffer = outputBuffer;
            m_InputBufferLength = inputBufferLength;
            m_OutputBufferLength = outputBufferLength;
            m_OutputBufferLength[0] = 0;
        }

        //========================================================================================================================
        //                                              Private - Search Node Related
        //========================================================================================================================


        public bool InsideImageBounds(int x, int y)
        {
            return (x >= 0) && (y >= 0) && (x < m_Width) && (y < m_Height);
        }

        public bool InsideBounds(int x, int y)
        {
            return (x >= -OutlineConstants.k_BorderSize) && (y >= -OutlineConstants.k_BorderSize) && (x < m_Width + OutlineConstants.k_BorderSize) && (y < m_Height + OutlineConstants.k_BorderSize);
        }

        int GetGridIndex(int x, int y)
        {
            return (x + OutlineConstants.k_BorderSize) + (y + OutlineConstants.k_BorderSize) * (m_Width + 2 * OutlineConstants.k_BorderSize);
        }

        public void AddOpen(ref OutlineSearchNode node)
        {
            if (IsUniqueNode(node.x, node.y))
            {
                m_InputBuffer[m_InputBufferLength[0]] = node;
                m_InputBufferLength[0] = (m_InputBufferLength[0] + 1) % m_InputBuffer.Length;
                m_InputLength++;

                int gridIndex = GetGridIndex(node.x, node.y);
                m_OpenCheckNodes[gridIndex] = node.shapeIndex;
            }
        }

        public bool HasInput()
        {
            return m_InputLength > 0;
        }

        public bool GetNextNode(ref OutlineSearchNode node)
        {
            node = m_InputBuffer[m_InputPosition];
            m_InputPosition = (m_InputPosition + 1) % m_InputBuffer.Length;
            m_InputLength--;

            int gridIndex = GetGridIndex(node.x, node.y);

            bool retValue = m_ClosedCheckNodes[gridIndex] == OutlineConstants.k_UninitializedOutlineId;
            m_ClosedCheckNodes[gridIndex] = m_OpenCheckNodes[gridIndex];
            m_OpenCheckNodes[gridIndex] = OutlineConstants.k_UninitializedOutlineId;

            return retValue;
        }


        public void AddInputNode(int x, int y, int shapeIndex)
        {
            // If we are out of bounds or are not unique return
            if (x < (0 - OutlineConstants.k_BorderSize) || x >= (m_Width + OutlineConstants.k_BorderSize) || y < (0 - OutlineConstants.k_BorderSize) || y >= (m_Height + OutlineConstants.k_BorderSize) || !IsUniqueNode(x, y))
                return;

            OutlineSearchNode node = new OutlineSearchNode();
            node.x = x;
            node.y = y;
            node.shapeIndex = shapeIndex;

            AddOpen(ref node);
        }

        public void AddOutputNode(ref OutlineSearchNode node)
        {
            m_OutputBuffer[m_OutputBufferLength[0]] = node;
            m_OutputBufferLength[0]++;
        }

        public void SetClosedCheckNode(ref OutlineSearchNode node)
        {
            int gridIndex = GetGridIndex(node.x, node.y);
            m_ClosedCheckNodes[gridIndex] = node.shapeIndex;
        }

        public void ClearOpenCheckNodes()
        {
            for (int y = 0; y < m_Height + 2 * OutlineConstants.k_BorderSize; y++)
            {
                int widthWithBorder = m_Width + 2 * OutlineConstants.k_BorderSize;
                for (int x = 0; x < widthWithBorder; x++)
                {
                    m_OpenCheckNodes[x + y * widthWithBorder] = OutlineConstants.k_UninitializedOutlineId;
                }
            }
        }

        public bool IsUniqueNode(int x, int y)
        {
            int gridIndex = GetGridIndex(x, y);
            return m_OpenCheckNodes[gridIndex] == OutlineConstants.k_UninitializedOutlineId && m_ClosedCheckNodes[gridIndex] == OutlineConstants.k_UninitializedOutlineId;
        }
    }
}
