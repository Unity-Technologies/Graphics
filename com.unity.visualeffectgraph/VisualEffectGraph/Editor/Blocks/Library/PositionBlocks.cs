using System;

namespace UnityEngine.Experimental.VFX
{
    class VFXSpawnOnSphereBlock : VFXBlockDesc
    {
        public VFXSpawnOnSphereBlock()
        {
            m_Properties = new VFXProperty[1] {
                VFXProperty.Create<VFXSphereType>("sphere"),
            };

            m_Attributes = new VFXAttribute[1] {
                new VFXAttribute("position",VFXValueType.kFloat3,true),
            };

            // TODO this should be derived automatically
            m_Flag = Flag.kHasRand;
            m_Hash = Hash128.Parse("1"); // dummy but must be unique
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

        public override string IconPath { get { return "Position"; } }
        public override string Name { get { return "Set Position (Sphere Surface)"; } }
        public override string Category { get { return "Tests/"; } }
    }
}