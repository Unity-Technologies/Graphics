using System;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.Experimental.VFX
{

    class VFXBlockSetColorConstant : VFXBlockType
    {
        public VFXBlockSetColorConstant()
        {
            Name = "Color (Constant)";
            Icon = "Color";
            Category = "Color";

            Add(VFXProperty.Create<VFXColorRGBType>("Color"));

            Add(new VFXAttribute(CommonAttrib.Color, true));

            Source = @"
color = Color;";
        }
    }

    class VFXBlockSetColorAlphaConstant : VFXBlockType
    {
        public VFXBlockSetColorAlphaConstant()
        {
            Name = "Color & Alpha (Constant)";
            Icon = "Color";
            Category = "Color";

            Add(VFXProperty.Create<VFXColorRGBType>("Color"));
            Add(VFXProperty.Create<VFXFloatType>("Alpha"));

            Add(new VFXAttribute(CommonAttrib.Color, true));

            Source = @"
color = Color;
alpha = Alpha;";
        }
    }

    class VFXBlockSetColorRandomUniform : VFXBlockType
    {
        public VFXBlockSetColorRandomUniform()
        {
            Name = "Color (Random uniform)";
            Icon = "Color";
            Category = "Color";

            Add(VFXProperty.Create<VFXColorRGBType>("FirstColor"));
            Add(VFXProperty.Create<VFXColorRGBType>("AltColor"));
            

            Add(new VFXAttribute(CommonAttrib.Color, true));

            Source = @"
color = lerp(FirstColor,AltColor,RAND);";
        }
    }

    class VFXBlockSetColorRandom : VFXBlockType
    {
        public VFXBlockSetColorRandom()
        {
            Name = "Color (Random, per-component)";
            Icon = "Color";
            Category = "Color";

            Add(VFXProperty.Create<VFXColorRGBType>("FirstColor"));
            Add(VFXProperty.Create<VFXColorRGBType>("AltColor"));
            

            Add(new VFXAttribute(CommonAttrib.Color, true));

            Source = @"
color = lerp(FirstColor,AltColor,RAND3);";
        }
    }

    class VFXBlockSetColorRandomGradient : VFXBlockType
    {
        public VFXBlockSetColorRandomGradient()
        {
            Name = "Color & Alpha (Random from Gradient)";
            Icon = "Color";
            Category = "Color";

            Add(VFXProperty.Create<VFXColorGradientType>("Gradient"));

            Add(new VFXAttribute(CommonAttrib.Color, true));
            Add(new VFXAttribute(CommonAttrib.Alpha, true));

            Source = @"
float4 rgba = SAMPLE(Gradient,RAND);
color = rgba.rgb;
alpha = rgba.a;"; 
        }
    }

    class VFXBlockSetAlphaRandom : VFXBlockType
    {
        public VFXBlockSetAlphaRandom()
        {
            Name = "Alpha (Random)";
            Icon = "Color";
            Category = "Color";

            Add(VFXProperty.Create<VFXFloatType>("MinAlpha"));
            Add(VFXProperty.Create<VFXFloatType>("MaxAlpha"));

            Add(new VFXAttribute(CommonAttrib.Color, true));

            Source = @"
alpha = lerp(MinAlpha,MaxAlpha);";
        }
    }

    class VFXBlockSetColorOverLifetime : VFXBlockType
    {
        public VFXBlockSetColorOverLifetime()
        {
            Name = "Color over Lifetime (Linear)";
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

    class VFXBlockSetAlphaOverLifetime : VFXBlockType
    {

        public VFXBlockSetAlphaOverLifetime()
        {
            Name = "Alpha over Lifetime (Linear)";
            Icon = "Color";
            Category = "Color";

            Add(new VFXProperty(new VFXFloatType(1.0f),"StartAlpha"));
            Add(new VFXProperty(new VFXFloatType(1.0f),"EndAlpha"));

            Add(new VFXAttribute(CommonAttrib.Alpha, true));
            Add(new VFXAttribute(CommonAttrib.Age, false));
            Add(new VFXAttribute(CommonAttrib.Lifetime, false));

            Source = @"
float ratio = saturate(age / lifetime);
alpha = lerp(StartAlpha,EndAlpha,ratio);";
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

    class VFXBlockColorFromTextureProjection : VFXBlockType
    {
        public VFXBlockColorFromTextureProjection()
        {
            Name = "Color from Texture (Projection)";
            Icon = "Color";
            Category = "Color";

            Add(VFXProperty.Create<VFXTexture2DType>("ColorTexture"));
            Add(VFXProperty.Create<VFXTransformType>("Transform"));

            Add(new VFXAttribute(CommonAttrib.Color, true));
            Add(new VFXAttribute(CommonAttrib.Alpha, true));
            Add(new VFXAttribute(CommonAttrib.Position, false));      

            Source = @"
                float4x4 t = transpose(Transform);
                float3 pos = position - t[3].xyz;
                float3 side = t[0];
                float u = dot(side,pos) / dot(side,side);
                float3 up = t[2];
                float v = dot(up,pos) / dot(up,up);
                float4 rgba = tex2Dlod(ColorTexture, float4(float2(u,v)+0.5,0,0));
                color = rgba.rgb;
                alpha = rgba.a;";
        }
    }
}
