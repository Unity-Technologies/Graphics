using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;

namespace UnityEngine.Experimental.Rendering.Universal
{
    internal struct IterativeLine
    {
        int m_IncrementX1;
        int m_IncrementY1;
        int m_IncrementX2;
        int m_IncrementY2;
        int m_LargestDelta;
        int m_SmallestDelta;
        int m_Error;
        int2 m_CurrentPos;
        int2 m_End;

        public void Initialize(int2 start, int2 end)
        {
            // Uses Breshenham's line drawing operate on points on the line
            int xSize = end.x - start.x;
            int ySize = end.y - start.y;

            m_IncrementX1 = 0;
            m_IncrementY1 = 0;
            m_IncrementX2 = 0;
            m_IncrementY2 = 0;

            if (xSize < 0)
                m_IncrementX1 = -1;
            else if (xSize > 0)
                m_IncrementX1 = 1;

            if (ySize < 0)
                m_IncrementY1 = -1;
            else if (ySize > 0)
                m_IncrementY1 = 1;

            m_LargestDelta = math.abs(xSize);
            m_SmallestDelta = math.abs(ySize);
            if (m_LargestDelta <= m_SmallestDelta)
            {
                int tmp = m_LargestDelta;
                m_LargestDelta = m_SmallestDelta;
                m_SmallestDelta = tmp;

                if (ySize < 0)
                    m_IncrementY2 = -1;
                else if (ySize > 0)
                    m_IncrementY2 = 1;
            }
            else
            {
                if (xSize < 0)
                    m_IncrementX2 = -1;
                else if (xSize > 0)
                    m_IncrementX2 = 1;
            }

            m_CurrentPos = start;
            m_Error = m_LargestDelta >> 1;
            m_End = end;
        }

        public int2 Current()
        {
            return m_CurrentPos;
        }

        public bool IsEnd()
        {
            return m_CurrentPos.x == m_End.x && m_CurrentPos.y == m_End.y;
        }

        public void Step()
        {
            m_Error += m_SmallestDelta;
            if (m_Error >= m_LargestDelta)
            {
                m_Error -= m_LargestDelta;
                m_CurrentPos.x += m_IncrementX1;
                m_CurrentPos.y += m_IncrementY1;
            }
            else
            {
                m_CurrentPos.x += m_IncrementX2;
                m_CurrentPos.y += m_IncrementY2;
            }
        }
    }
}
