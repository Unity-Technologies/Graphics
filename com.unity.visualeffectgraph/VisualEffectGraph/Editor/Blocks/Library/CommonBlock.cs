using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.Experimental.VFX
{
    class VFXBlockCommonAgeAndReap : VFXBlockType
    {
        public VFXBlockCommonAgeAndReap()
        {
            Name = "Age And Reap";
            Icon = "Time";
            Category = "";

            Add(new VFXAttribute(CommonAttrib.Age, true));
            Add(new VFXAttribute(CommonAttrib.Lifetime, false));


            Source = @"
age += deltaTime;
if (age >= lifetime)
	KILL;";
        }
    }

    class VFXBlockCommonIntegrateVelocity : VFXBlockType
    {
        public VFXBlockCommonIntegrateVelocity()
        {
            Name = "Integrate Velocity (Constant)";
            Icon = "Position";
            Category = "";

            Add(new VFXAttribute(CommonAttrib.Position, true));
            Add(new VFXAttribute(CommonAttrib.Velocity, false));


            Source = @"
position += velocity * deltaTime;
";
        }
    }

    class VFXBlockCommonIntegrateVelocityCurve : VFXBlockType
    {
        public VFXBlockCommonIntegrateVelocityCurve()
        {
            Name = "Integrate Velocity (Curve)";
            Icon = "Position";
            Category = "";

            Add(VFXProperty.Create<VFXCurveType>("Curve"));

            Add(new VFXAttribute(CommonAttrib.Age, false));
            Add(new VFXAttribute(CommonAttrib.Lifetime, false));
            Add(new VFXAttribute(CommonAttrib.Position, true));
            Add(new VFXAttribute(CommonAttrib.Velocity, false));

            Source = @"
float ratio = saturate(age/lifetime);
float vscale = SAMPLE(Curve,ratio);
position += velocity * vscale * deltaTime;";
        }
    }
}
