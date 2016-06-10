using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.Experimental.VFX
{
    public class VFXBlockAngleConstant : VFXBlockType
    {
        public VFXBlockAngleConstant()
        {
            Name = "Constant";
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
            Name = "Random";
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
            Name = "Constant";
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
            Name = "Random";
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
            Name = "Force (Constant)";
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

    public class VFXBlockSetPivot : VFXBlockType
    {
        public VFXBlockSetPivot()
        {
            Name = "Set Pivot";
            Icon = "";
            Category = "Pivot";

            Add(VFXProperty.Create<VFXFloat3Type>("Pivot"));

            Add(new VFXAttribute(CommonAttrib.Pivot, true));

            Source = @"
pivot = Pivot;";
        }
    }
}
