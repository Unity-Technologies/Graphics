using System;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.Experimental.VFX
{
    class VFXBlockSetSubUVRandom : VFXBlockType
    {
        public VFXBlockSetSubUVRandom()
        {
            Name = "Set Flipbook Index (Random)";
            Icon = "Flipbook";
            Category = "Flipbook";

            Add(VFXProperty.Create<VFXFloatType>("MinIndex"));
            Add(VFXProperty.Create<VFXFloatType>("MaxIndex"));

            Add(new VFXAttribute(CommonAttrib.TexIndex, true));

            Source = @"
texIndex = lerp(MinIndex,MaxIndex,RAND);";
        }
    }

    class VFXBlockSubUVAnimateConstant : VFXBlockType
    {
        public VFXBlockSubUVAnimateConstant()
        {
            Name = "Animate Flipbook (Constant)";
            Icon = "Flipbook";
            Category = "Flipbook";

            Add(VFXProperty.Create<VFXFloatType>("Framerate"));

            Add(new VFXAttribute(CommonAttrib.TexIndex, true));

            Source = @"
texIndex += Framerate * DeltaTime;";
        }
    }

    class VFXBlockSubUVAnimateCurve : VFXBlockType
    {
        public VFXBlockSubUVAnimateCurve()
        {
            Name = "Animate Flipbook (Curve)";
            Icon = "Flipbook";
            Category = "Flipbook";

            Add(VFXProperty.Create<VFXCurveType>("Curve"));

            Add(new VFXAttribute(CommonAttrib.Age, false));
            Add(new VFXAttribute(CommonAttrib.Lifetime, false));
            Add(new VFXAttribute(CommonAttrib.TexIndex, true));

            Source = @"
float r = saturate(age/lifetime);
texIndex = SAMPLE(Curve, r);";
        }
    }


}
