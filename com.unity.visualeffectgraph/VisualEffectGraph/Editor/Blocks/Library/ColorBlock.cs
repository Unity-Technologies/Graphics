using System;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.Experimental.VFX
{
    class VFXBlockSetColorOverLifetime : VFXBlockType
    {
        public VFXBlockSetColorOverLifetime()
        {
            Name = "Color over Lifetime (2 Colors)";
            Icon = "Color";
            Category = "Color";

            Add(VFXProperty.Create<VFXColorRGBType>("StartColor"));
            Add(VFXProperty.Create<VFXColorRGBType>("EndColor"));

            Add(new VFXAttribute(CommonAttrib.Color, true));
            Add(new VFXAttribute(CommonAttrib.Age, false));
            Add(new VFXAttribute(CommonAttrib.Lifetime, false));

            Source = @"
float ratio = saturate(age / lifetime);
color = lerp(StartColor,EndColor,ratio);";
        }
    }

    class VFXBlockSetColorGradientOverLifetime : VFXBlockType
    {
        public VFXBlockSetColorGradientOverLifetime()
        {
            Name = "Color over Lifetime (RGBA Gradient)";
            Icon = "Color";
            Category = "Color";

            Add(VFXProperty.Create<VFXColorGradientType>("Gradient"));

            Add(new VFXAttribute(CommonAttrib.Color, true));
            Add(new VFXAttribute(CommonAttrib.Alpha, true));
            Add(new VFXAttribute(CommonAttrib.Age, false));
            Add(new VFXAttribute(CommonAttrib.Lifetime, false));

            Source = @"
float ratio = saturate(age / lifetime);
float4 rgba = SAMPLE(Gradient,ratio);
color = rgba.rgb;
alpha = rgba.a;"; 
        }
    }

    class VFXBlockSetAlphaCurveOverLifetime : VFXBlockType
    {
        public VFXBlockSetAlphaCurveOverLifetime()
        {
            Name = "Alpha over Lifetime (Curve)";
            Icon = "Color";
            Category = "Color";

            Add(VFXProperty.Create<VFXCurveType>("Curve"));

            Add(new VFXAttribute(CommonAttrib.Alpha, true));
            Add(new VFXAttribute(CommonAttrib.Age, false));
            Add(new VFXAttribute(CommonAttrib.Lifetime, false));

            Source = @"
float ratio = saturate(age / lifetime);
alpha = SAMPLE(Curve,ratio);";
        }
    }
}
