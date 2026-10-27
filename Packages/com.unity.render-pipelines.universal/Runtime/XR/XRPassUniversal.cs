using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    internal class XRPassUniversal : XRPass
    {
        public static XRPass Create(XRPassCreateInfo createInfo)
        {
            XRPassUniversal pass = GenericPool<XRPassUniversal>.Get();
            pass.InitBase(createInfo);

            // Initialize fields specific to Universal
            pass.isLateLatchEnabled = false;
            pass.canMarkLateLatch = false;
            pass.hasMarkedLateLatch = false;
            pass.canFoveateIntermediatePasses = true;
            pass.stackCullingResults.Clear();
            pass.stackCameraIndex = 0;

            return pass;
        }

        override public void Release()
        {
            GenericPool<XRPassUniversal>.Release(this);
        }

        /// If true, late latching mechanism is available for the frame.
        internal bool isLateLatchEnabled { get; set; }

        /// Used by the render pipeline to control the granularity of late latching.
        internal bool canMarkLateLatch { get; set; }

        /// Track the state of the late latching system.
        internal bool hasMarkedLateLatch { get; set; }

        /// If false, foveated rendering should not be applied to intermediate render passes that are not the final pass.
        internal bool canFoveateIntermediatePasses { get; set; }

        /// Culling results of each camera in the stack, indexed by stackCameraIndex. Only used with Quad Views, where the
        /// outer pass fills them and the inner pass reuses them.
        internal readonly List<CullingResults?> stackCullingResults = new List<CullingResults?>();

        /// Index of the camera currently being rendered (0 is the base camera, i + 1 is overlay i)
        internal int stackCameraIndex;

        internal bool TryGetStackCullingResults(out CullingResults cullingResults)
        {
            if (stackCameraIndex < stackCullingResults.Count && stackCullingResults[stackCameraIndex].HasValue)
            {
                cullingResults = stackCullingResults[stackCameraIndex].Value;
                return true;
            }

            cullingResults = default;
            return false;
        }

        internal void SetStackCullingResults(CullingResults cullingResults)
        {
            while (stackCullingResults.Count <= stackCameraIndex)
                stackCullingResults.Add(null);

            stackCullingResults[stackCameraIndex] = cullingResults;
        }
    }
}
