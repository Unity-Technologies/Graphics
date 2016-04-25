using System;

namespace UnityEngine.Experimental.VFX
{
    class VFXBlockSetPositionPoint : VFXBlockDesc
    {
        public VFXBlockSetPositionPoint()
        {
            m_Properties = new VFXProperty[1] {
                VFXProperty.Create<VFXFloat3Type>("position"),
            };

            m_Attributes = new VFXAttribute[1] {
                new VFXAttribute("position",VFXValueType.kFloat3,true),
            };

            // TODO this should be derived automatically
            m_Flag = 0;
            m_Hash = Hash128.Parse(Name); // dummy but must be unique
        }

        public override string Source
        {
            get { return "position = value;"; }
        }

        public override string Name     { get { return "Set Position (Point)"; } }
        public override string IconPath { get { return "Position"; } }     
        public override string Category { get { return "Position/"; } }
    }

    class VFXBlockSetPositionMap : VFXBlockDesc
    {
        public VFXBlockSetPositionMap()
        {
            m_Properties = new VFXProperty[3] {
                VFXProperty.Create<VFXTexture2DType>("tex"),
                VFXProperty.Create<VFXAABoxType>("box"),
                VFXProperty.Create<VFXFloatType>("divergence")
            };

            m_Attributes = new VFXAttribute[1] {
                new VFXAttribute("position",VFXValueType.kFloat3,true),
            };

            // TODO this should be derived automatically
            m_Flag = Flag.kHasRand;
            m_Hash = Hash128.Parse(Name); // dummy but must be unique
        }

        public override string Source
        {
            get 
            { 
                return @"float3 div = divergence * 2.0f * (float3(rand(seed),rand(seed),rand(seed)) - 0.5f);
    position = box_center + ((div + tex2Dlod(tex,float4(rand(seed),rand(seed),0,0)).rgb - 0.5f) * box_size);"; 
            }
        }

        public override string Name { get { return "Set Position (Texture)"; } }
        public override string IconPath { get { return "Position"; } }
        public override string Category { get { return "Position/"; } }
    }

    class VFXBlockSetPositionBox : VFXBlockDesc
    {
        public VFXBlockSetPositionBox()
        {
            m_Properties = new VFXProperty[1] {
                VFXProperty.Create<VFXAABoxType>("box"),
            };

            m_Attributes = new VFXAttribute[1] {
                new VFXAttribute("position",VFXValueType.kFloat3,true),
            };

            // TODO this should be derived automatically
            m_Flag = Flag.kHasRand;
            m_Hash = Hash128.Parse(Name); // dummy but must be unique
        }

        public override string Source
        {
            get 
            {
                return @"box_size *= 0.5f;
    position = float3(  lerp(box_center.x + box_size.x, box_center.x - box_size.x, rand(seed)),
	                    lerp(box_center.y + box_size.y, box_center.y - box_size.y, rand(seed)),
	                    lerp(box_center.z + box_size.z, box_center.z - box_size.z, rand(seed)));";
            }
        }

        public override string Name { get { return "Set Position (Box)"; } }
        public override string IconPath { get { return "Position"; } }
        public override string Category { get { return "Position/"; } }
    }

    class VFXBlockSetPositionSphereSurface : VFXBlockDesc
    {
        public VFXBlockSetPositionSphereSurface()
        {
            m_Properties = new VFXProperty[1] {
                VFXProperty.Create<VFXSphereType>("sphere"),
            };

            m_Attributes = new VFXAttribute[1] {
                new VFXAttribute("position",VFXValueType.kFloat3,true),
            };

            // TODO this should be derived automatically
            m_Flag = Flag.kHasRand;
            m_Hash = Hash128.Parse(Name); // dummy but must be unique
        }

        public override string Source
        {
            get
            {
                return @"float u1 = 2.0 * rand(seed) - 1.0;
    float u2 = UNITY_TWO_PI * rand(seed);
    float2 sincosTheta;
    sincos(u2,sincosTheta.x,sincosTheta.y);
    sincosTheta *= sqrt(1.0 - u1*u1);
    position = (float3(sincosTheta,u1) * sphere_radius) + sphere_center;";
            }
        }

        public override string Name { get { return "Set Position (Sphere Surface)"; } }
        public override string IconPath { get { return "Position"; } }
        public override string Category { get { return "Position/"; } }
    }

    // TMP to test color
    class VFXBlockSetColorOverLifetime : VFXBlockDesc
    {
        public VFXBlockSetColorOverLifetime()
        {
            m_Properties = new VFXProperty[2] {
                VFXProperty.Create<VFXColorRGBType>("start"),
                VFXProperty.Create<VFXColorRGBType>("end"),
            };

            m_Attributes = new VFXAttribute[3] {
                new VFXAttribute("color",VFXValueType.kFloat3,true),
                new VFXAttribute("age",VFXValueType.kFloat,false),
                new VFXAttribute("lifetime",VFXValueType.kFloat,false),
            };

            // TODO this should be derived automatically
            m_Flag = Flag.kHasRand;
            m_Hash = Hash128.Parse(Name); // dummy but must be unique
        }

        public override string Source
        {
            get
            {
                return @"float ratio = saturate(age / lifetime);
    color = lerp(start,end,ratio);";
            }
        }

        public override string Name { get { return "Color Over Lifetime"; } }
        public override string IconPath { get { return "Color"; } }
        public override string Category { get { return "Color/"; } }
    }

}