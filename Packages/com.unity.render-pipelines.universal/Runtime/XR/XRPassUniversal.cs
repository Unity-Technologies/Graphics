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
    }
}
