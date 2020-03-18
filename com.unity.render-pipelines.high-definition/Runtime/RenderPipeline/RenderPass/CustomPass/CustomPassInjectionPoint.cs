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
        BeforeRendering             = 0,
        AfterOpaqueDepthAndNormal   = 5,
        BeforePreRefraction         = 4,
        BeforeTransparent           = 1,
        BeforePostProcess           = 2,
        AfterPostProcess            = 3,
    }
}