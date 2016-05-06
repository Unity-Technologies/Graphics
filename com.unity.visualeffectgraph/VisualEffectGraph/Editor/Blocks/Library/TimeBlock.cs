using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.Experimental.VFX
{
    class VFXBlockSetLifetimeConstant : VFXBlockType
    {
        public VFXBlockSetLifetimeConstant()
        {
            Name = "Set Lifetime (Constant)";
            Icon = "Time";
            Category = "Time";

            Add(new VFXProperty(new VFXFloatType(1.0f),"Lifetime"));

            Add(new VFXAttribute(CommonAttrib.Lifetime, true));

            Source = @"
lifetime = max(Lifetime,0.0f);";
        }
    }

    class VFXBlockSetLifetimeRandom : VFXBlockType
    {
        public VFXBlockSetLifetimeRandom()
        {
            Name = "Set Lifetime (Random)";
            Icon = "Time";
            Category = "Time";

            Add(new VFXProperty(new VFXFloatType(0.25f),"MinLifetime"));
            Add(new VFXProperty(new VFXFloatType(1.0f),"MaxLifetime"));

            Add(new VFXAttribute(CommonAttrib.Lifetime, true));

            Source = @"
lifetime = max(MinLifetime + RAND * (MaxLifetime-MinLifetime),0.0f);";
        }
    }

    class VFXBlockSetLifetimeCurve : VFXBlockType
    {
        public VFXBlockSetLifetimeCurve()
        {
            Name = "Set Lifetime (Curve)";
            Icon = "Time";
            Category = "Time";

            Add(VFXProperty.Create<VFXCurveType>("Curve"));

            Add(new VFXAttribute(CommonAttrib.Lifetime, true));

            Source = @"
lifetime = SAMPLE(Curve,totalTime);";
        }
    }
}
