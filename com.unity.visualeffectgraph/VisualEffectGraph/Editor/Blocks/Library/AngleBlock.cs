using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.Experimental.VFX
{
    public class VFXBlockAngleConstant : VFXBlockType
    {
        public VFXBlockAngleConstant()
        {
            Name = "Set Angle (Constant)";
            Icon = "Angle";
            Category = "Angle";

            Add(new VFXProperty(new VFXFloatType(180f),"Value"));

            Add(new VFXAttribute(CommonAttrib.Angle, true));

            Source = @"
angle += Value;";
        }
    }

    public class VFXBlockAngleRandom : VFXBlockType
    {
        public VFXBlockAngleRandom()
        {
            Name = "Set Angle (Random)";
            Icon = "Angle";
            Category = "Angle";

            Add(new VFXProperty(new VFXFloatType(0f),"Min"));
            Add(new VFXProperty(new VFXFloatType(360f),"Max"));

            Add(new VFXAttribute(CommonAttrib.Angle, true));

            Source = @"
angle += lerp(Min,Max,RAND);";
        }
    }

    public class VFXBlockAngularVelocityConstant : VFXBlockType
    {
        public VFXBlockAngularVelocityConstant()
        {
            Name = "Set Spin (Constant)";
            Icon = "Angle";
            Category = "Angle/Spin";

            Add(new VFXProperty(new VFXFloatType(30f),"Value"));

            Add(new VFXAttribute(CommonAttrib.AngularVelocity, true));

            Source = @"
angularVelocity = Value;";
        }
    }

    public class VFXBlockAngularVelocityRandom : VFXBlockType
    {
        public VFXBlockAngularVelocityRandom()
        {
            Name = "Set Spin (Random)";
            Icon = "Angle";
            Category = "Angle/Spin";

            Add(new VFXProperty(new VFXFloatType(-30f),"Min"));
            Add(new VFXProperty(new VFXFloatType(30f),"Max"));

            Add(new VFXAttribute(CommonAttrib.AngularVelocity, true));

            Source = @"
angularVelocity = lerp(Min,Max,RAND);";
        }
    }
    public class VFXBlockAngularForceConstant : VFXBlockType
    {
        public VFXBlockAngularForceConstant()
        {
            Name = "Spin Force (Constant)";
            Icon = "Angle";
            Category = "Angle/Spin";

            Add(new VFXProperty(new VFXFloatType(30f),"Value"));

            Add(new VFXAttribute(CommonAttrib.AngularVelocity, true));

            Source = @"
angularVelocity += Value * deltaTime;";
        }
    }

    public class VFXBlockAngularDrag : VFXBlockType
    {
        public VFXBlockAngularDrag()
        {
            Name = "Spin Drag (Constant)";
            Icon = "Angle";
            Category = "Angle/Spin";

            Add(new VFXProperty(new VFXFloatType(1f),"Value"));

            Add(new VFXAttribute(CommonAttrib.AngularVelocity, true));

            Source = @"
angularVelocity *= max(0.0,(1.0 - Value * deltaTime));";
        }
    }
}
