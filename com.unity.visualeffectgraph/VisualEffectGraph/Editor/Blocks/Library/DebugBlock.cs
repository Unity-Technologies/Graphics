using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.Experimental.VFX
{
    class VFXBlockDebugTestCompileFailure : VFXBlockType
    {
        public VFXBlockDebugTestCompileFailure()
        {
            Name = "COMPILE FAILURE";
            Icon = "Position";
            Category = "ZZ_DEBUG";

            Source = @"
THIS CODE WILL SURELY NOT COMPILE!;";
        }
    }

    class VFXBlockDebugTestMissingIcon : VFXBlockType
    {
        public VFXBlockDebugTestMissingIcon()
        {
            Name = "MISSING ICON";
            Icon = "__MISSING__ICON__HERE___";
            Category = "ZZ_DEBUG";

            Source = @"
float a = 1;";
        }
    }

    class VFXBlockDebugPhaseToColor : VFXBlockType
    {
        public VFXBlockDebugPhaseToColor()
        {
            Name = "SamplingCorrection to Color";
            Icon = "Color";
            Category = "ZZ_DEBUG";

            Add(new VFXAttribute(CommonAttrib.Color, true));
            Add(new VFXAttribute(CommonAttrib.Phase, false));

            Source = @"
color = float3(phase,1 - phase,0.0);";
        }
    }
}
