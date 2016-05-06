using System;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.Experimental.VFX
{
    class VFXBlockSetPositionPoint : VFXBlockType
    {
        public VFXBlockSetPositionPoint()
        {
            Name = "Set Position (Point)";
            Icon = "Position";
            Category = "Position";

            Add(VFXProperty.Create<VFXPositionType>("pos"));

            Add(new VFXAttribute(CommonAttrib.Position, true));

            Source = @"
position = value;";
        }
    }

    class VFXBlockSetPositionMap : VFXBlockType
    {
        public VFXBlockSetPositionMap()
        {
            Name = "Set Position (Texture)";
            Icon = "Position";
            Category = "Position";

            Add(VFXProperty.Create<VFXTexture2DType>("tex"));
            Add(VFXProperty.Create<VFXOrientedBoxType>("box"));
            Add(VFXProperty.Create<VFXFloatType>("divergence"));

            Add(new VFXAttribute(CommonAttrib.Position, true));

            Source = @"
float3 div = (RAND3 - 0.5f) * (divergence * 2.0f);
position = (div + tex2Dlod(tex,float4(RAND2,0,0)).rgb) - 0.5f;
position = mul(box,float4(position.xyz,1.0f)).xyz;"; 
        }
    }

    class VFXBlockSetPositionAABox : VFXBlockType
    {
        public VFXBlockSetPositionAABox()
        {
            Name = "Set Position (AABox)";
            Icon = "Position";
            Category = "Position";

            Add(VFXProperty.Create<VFXAABoxType>("aabox"));

            Add(new VFXAttribute(CommonAttrib.Position, true));

            Source = @"
float3 minCoord = (aabox_size * -0.5f) + aabox_center;
position = (RAND3 * aabox_size) + minCoord;";
        }
    }

    class VFXBlockSetPositionBox : VFXBlockType
    {
        public VFXBlockSetPositionBox()
        {
            Name = "Set Position (Oriented Box)";
            Icon = "Position";
            Category = "Position";

            Add(VFXProperty.Create<VFXOrientedBoxType>("box"));

            Add(new VFXAttribute(CommonAttrib.Position, true));

            Source = @"
position = RAND3 - 0.5f;
position = mul(box,float4(position,1.0f)).xyz;";
        }
    }

    class VFXBlockSetPositionSphereSurface : VFXBlockType
    {
        public VFXBlockSetPositionSphereSurface()
        {
            Name = "Set Position (Sphere surface)";
            Icon = "Position";
            Category = "Position";

            Add(VFXProperty.Create<VFXSphereType>("Sphere"));

            Add(new VFXAttribute(CommonAttrib.Position, true));

            Source = @"
float u1 = 2.0 * RAND - 1.0;
float u2 = UNITY_TWO_PI * RAND;
float2 sincosTheta;
sincos(u2,sincosTheta.x,sincosTheta.y);
sincosTheta *= sqrt(1.0 - u1*u1);
position = (float3(sincosTheta,u1) * Sphere_radius) + Sphere_center;";
        }
    }

    class VFXBlockSetPositionSphereVolume : VFXBlockType
    {
        public VFXBlockSetPositionSphereVolume()
        {
            Name = "Set Position (Sphere volume)";
            Icon = "Position";
            Category = "Position";

            Add(VFXProperty.Create<VFXSphereType>("Sphere"));

            Add(new VFXAttribute(CommonAttrib.Position, true));

            Source = @"
float u1 = 2.0 * RAND - 1.0;
float u2 = UNITY_TWO_PI * RAND;
float u3 = pow(RAND,1.0/3.0);
float2 sincosTheta;
sincos(u2,sincosTheta.x,sincosTheta.y);
sincosTheta *= sqrt(1.0 - u1*u1);
position = float3(sincosTheta,u1) * (u3 * Sphere_radius) + Sphere_center;";
        }
    }

    class VFXBlockTransformPosition : VFXBlockType
    {
        public VFXBlockTransformPosition()
        {
            Name = "Transform Position";
            Icon = "Position";
            Category = "Position";

            Add(VFXProperty.Create<VFXTransformType>("transform"));

            Add(new VFXAttribute(CommonAttrib.Position, true));

            Source = @"
position = mul(transform,float4(position,1.0f)).xyz;";
        }
    }

   



    // TODO Convert that in some other files
/*
    class VFXBlockTransformVelocity : VFXBlockDesc
    {
        public VFXBlockTransformVelocity()
        {
            m_Properties = new VFXProperty[1] {
                VFXProperty.Create<VFXTransformType>("transform"),
            };

            m_Attributes = new VFXAttribute[1] {
                new VFXAttribute("velocity",VFXValueType.kFloat3,true),
            };

            // TODO this should be derived automatically
            m_Flag = Flag.kNone;
            m_Hash = Hash128.Parse(Name); // dummy but must be unique
        }

        public override string Source
        {
            get
            {
                return @"velocity = mul(transform,float4(velocity,0.0f)).xyz;";
            }
        }

        public override string Name { get { return "Transform Velocity"; } }
        public override string IconPath { get { return "Velocity"; } }
        public override string Category { get { return "Velocity/"; } }
    }

    

    class VFXBlockCurveTest : VFXBlockDesc
    {
        public VFXBlockCurveTest()
        {
            m_Properties = new VFXProperty[] {
                VFXProperty.Create<VFXCurveType>("curve"),
            };

            m_Attributes = new VFXAttribute[] {
                new VFXAttribute("position",VFXValueType.kFloat3,true),
            };

            // TODO this should be derived automatically
            m_Flag = Flag.kNone;
            m_Hash = Hash128.Parse(Name); // dummy but must be unique
        }

        public override string Source
        {
            get
            {
                return @"float dist = length(position.xz);
    position.y = SAMPLE(curve,dist);";
            }
        }

        public override string Name { get { return "Test Curve"; } }
        public override string IconPath { get { return "Curve"; } }
        public override string Category { get { return "Tests/"; } }
    }*/
}
