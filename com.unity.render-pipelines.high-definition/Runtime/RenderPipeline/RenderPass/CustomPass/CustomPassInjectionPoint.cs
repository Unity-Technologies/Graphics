using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// List all the injection points available for HDRP
    /// </summary>
    [GenerateHLSL]
    public enum CustomPassInjectionPoint
    {
        // Important: don't touch the value of the injection points for the serialization.
        // Ordered by injection point in the frame for the enum popup in the UI.

        /// <summary>Just after HDRP clears the depth buffer. You can write to the depth buffer to make HDRP not render depth-tested opaque GameObjects.</summary>
        BeforeRendering             = 0,
        /// <summary>At this point, you can modify the normal, roughness, and depth buffer. If you write to these buffers at this injection point, HDRP takes it into account in the lighting and the depth pyramid.</summary>
        AfterOpaqueDepthAndNormal   = 5,
        /// <summary>At this injection point, you can render any transparent GameObject that you want to see in refraction. If you write to buffers at this injection point, they contents end up in the color pyramid that HDRP uses for refraction when it draws transparent GameObjects.</summary>
        BeforePreRefraction         = 4,
        /// <summary>At this injection point, you can sample the color pyramid that HDRP generates for rough transparent refraction.</summary>
        BeforeTransparent           = 1,
        /// <summary>This injection point is after HDRP handles post-processesing. At this point, depth is jittered which means you cannot draw depth tested GameObjects without having artifacts.</summary>
        BeforePostProcess           = 2,
        /// <summary>This injection point is before HDRP renders post-processing and custom post-processing effects.</summary>
        AfterPostProcess            = 3,
    }
}