using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;


namespace UnityEngine.Rendering.Universal
{
    internal struct ShadowEdgeLookupTable : IDisposable
    {
        const int k_EmptyIndex = -1;

        public struct EdgeLookupElement
        {
            public int edgeIndex;
            public int nextElement;

            public EdgeLookupElement(int inValue, int inNext)
            {
                edgeIndex = inValue;
                nextElement = inNext;
            }
        }

        NativeArray<EdgeLookupElement> m_LookupTable;
        int m_TableSize;
        int m_TableAddPosition;
        IntPtr m_LookupTablePtr;


        public int size => m_TableSize;

        public void Add(int inIndex, int inValue)
        {
            unsafe
            {
                int currentElement = inIndex;

                EdgeLookupElement* lookupTablePtr = (EdgeLookupElement*)m_LookupTablePtr.ToPointer();


                int indexToWrite = inValue;
                while (lookupTablePtr[currentElement].edgeIndex != k_EmptyIndex)
                {
                    if (lookupTablePtr[currentElement].nextElement != k_EmptyIndex)
                    {
                        // We are adding to the front of our list and need to move everything back.
                        int temp = indexToWrite;
                        indexToWrite = lookupTablePtr[currentElement].edgeIndex;
                        lookupTablePtr[currentElement] = new EdgeLookupElement(temp, lookupTablePtr[currentElement].nextElement);
                        currentElement = lookupTablePtr[currentElement].nextElement;
                    }
                    else
                    {
                        // Add a new element and connect it to the last element in our list
                        int temp = indexToWrite;
                        indexToWrite = lookupTablePtr[currentElement].edgeIndex;
                        lookupTablePtr[currentElement] = new EdgeLookupElement(temp, m_TableAddPosition);
                        currentElement = m_TableAddPosition;
                        m_TableAddPosition--;
                    }
                }

                // Write to the back of our list
                lookupTablePtr[currentElement] = new EdgeLookupElement(indexToWrite, k_EmptyIndex);
            }
        }

        public int DepthAt(int inIndex)
        {
            int retValue = 0;

            unsafe
            {
                if (inIndex <= m_TableAddPosition)
                {
                    int currentElement = inIndex;

                    EdgeLookupElement* lookupTablePtr = (EdgeLookupElement*)m_LookupTablePtr.ToPointer();

                    // Go to the end of our list
                    int count = 0;
                    while (lookupTablePtr[currentElement].nextElement != k_EmptyIndex)
                    {
                        count++;
                        currentElement = lookupTablePtr[currentElement].nextElement;
                    }

                    if (lookupTablePtr[currentElement].edgeIndex != k_EmptyIndex)
                        retValue = count + 1;
                }
            }

            return retValue;
        }

        public int GetValueAt(int inIndex, int inDepth)
        {
            unsafe
            {
                if (inIndex <= m_TableAddPosition)
                {
                    int currentElement = inIndex;

                    EdgeLookupElement* lookupTablePtr = (EdgeLookupElement*)m_LookupTablePtr.ToPointer();

                    // Go to the end of our list
                    for(int i=0;i<inDepth;i++)
                    {
                        int nextElement = lookupTablePtr[currentElement].nextElement;
                        if (nextElement == k_EmptyIndex)
                            return k_EmptyIndex;

                        currentElement = nextElement;
                    }

                    return lookupTablePtr[currentElement].edgeIndex;
                }
            }

            return k_EmptyIndex;
        }


        public void Initialize(int inSize)
        {
            m_LookupTable = new NativeArray<EdgeLookupElement>(inSize, Allocator.Persistent);
            m_TableAddPosition = inSize - 1;
            m_TableSize = inSize;

            unsafe
            {
                m_LookupTablePtr = new IntPtr(m_LookupTable.m_Buffer);
                EdgeLookupElement* lookupTablePtr = (EdgeLookupElement*)m_LookupTablePtr.ToPointer();
                EdgeLookupElement initalValue = new EdgeLookupElement(k_EmptyIndex, k_EmptyIndex);
                for (int i = 0; i < inSize; i++)
                    lookupTablePtr[i] = initalValue;
            }
        }

        public void Dispose()
        {
            m_LookupTable.Dispose();
        }
    }
}
