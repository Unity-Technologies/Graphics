using System.Collections.Generic;
using UnityEngine;
using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public class RenderingDebugSettings
    {
        public bool displayOpaqueObjects = true;
        public bool displayTransparentObjects = true;
        public bool enableDistortion = true;
        public bool enableGaussianPyramid = true;
        public bool enableSSSAndTransmission = true;
        public bool enableAtmosphericScattering = true;
        public bool allowStereo = true;
    }
}
