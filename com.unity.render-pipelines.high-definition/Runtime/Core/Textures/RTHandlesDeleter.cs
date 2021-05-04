using System.Collections.Generic;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// RTHandleDeleter to schedule a release of a RTHandle in N ('lifetime') frame
    /// </summary>
    internal static class RTHandlesDeleter
    {
        internal class RTHandleDesc
        {
            public int         lifetime = 3;
            public RTHandle    rtHandle = null;
        }

        internal static List<RTHandleDesc> m_RTHandleDescs = new List<RTHandleDesc>();

        /// <summary>
        /// Schedule a release of a RTHandle in 'lifetime' frames
        /// </summary>
        /// <param name="rtHandle">Considered rtHandle.</param>
        /// <param name="lifetime">lifetime remaining of this rtHandle (unit: frame), default: 3 frames.</param>
        internal static void ScheduleRelease(RTHandle rtHandle, int lifetime = 3)
        {
            if (rtHandle != null && lifetime > 0)
            {
                RTHandleDesc desc = new RTHandleDesc();
                desc.lifetime = lifetime;
                desc.rtHandle = rtHandle;
                m_RTHandleDescs.Add(desc);
            }
            else
            {
                rtHandle?.Release();
            }
        }

        /// <summary>
        /// Schedule a release of a RTHandle in 'lifetime' frames
        /// </summary>
        internal static void Update()
        {
            foreach (RTHandleDesc desc in m_RTHandleDescs)
            {
                --desc.lifetime;
            }

            // Release 'too old' RTHandle
            for (int i = m_RTHandleDescs.Count - 1; i >= 0; i--)
            {
                var cur = m_RTHandleDescs[i];
                if (cur.lifetime <= 0)
                {
                    cur.rtHandle.Release();
                    m_RTHandleDescs.Remove(cur);
                }
            }
        }
    }
}
