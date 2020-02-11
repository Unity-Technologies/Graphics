using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// RTHandleDeleter to schedule a release of a RTHandle in N ('lifetime') frame
    /// </summary>
    public static class RTHandleDeleter
    {
        internal class RTHandleDesc
        {
            public int         lifetime = 5;
            public RTHandle    rtHandle = null;
        }

        internal static List<RTHandleDesc> m_RTHandleDescs = new List<RTHandleDesc>();

        /// <summary>
        /// Schedule a release of a RTHandle in 'lifetime' frames
        /// </summary>
        /// <param name="rtHandle">Considered rtHandle.</param>
        /// <param name="lifetime">lifetime remaining of this rtHandle (unit: frame), default: 5 frames.</param>
        public static void ScheduleRelease(RTHandle rtHandle, int lifetime = 2)
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
        public static void Update()
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
