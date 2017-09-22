using System;
using System.Linq;
using System.Collections.Generic;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Position")]
    class VFXPositionSphere : VFXBlock
    {
        // TODO: Let's factorize this this into a utility class
        public enum PrimitivePositionMode
        {
            Surface,
            Volume,
        }

        [VFXSetting]
        public PrimitivePositionMode SpawnMode;
        [VFXSetting]
        public bool ApplySpeed;

        public override string name { get { return "Position: Sphere"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kInitAndUpdateAndOutput; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }
        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.ReadWrite);
                yield return new VFXAttributeInfo(VFXAttribute.Seed, VFXAttributeMode.ReadWrite);
                if (ApplySpeed)
                    yield return new VFXAttributeInfo(VFXAttribute.Velocity, VFXAttributeMode.ReadWrite);
            }
        }

        public class InputProperties
        {
            public Sphere Sphere = new Sphere() { radius = 1.0f };
            public float Speed = 1.0f;
        }

        public override string source
        {
            get
            {
                string out_source = "";
                switch(SpawnMode)
                {
                    case PrimitivePositionMode.Surface: out_source += @"
float u1 = 2.0 * RAND - 1.0;
float u2 = UNITY_TWO_PI * RAND;
float3 pos = VFXPositionOnSphereSurface(Sphere,u1,u2);
position += pos;";
                        break;
                    case PrimitivePositionMode.Volume: out_source += @"
float u1 = 2.0 * RAND - 1.0;
float u2 = UNITY_TWO_PI * RAND;
float u3 = pow(RAND,1.0/3.0);
float3 pos = VFXPositionOnSphere(Sphere,u1,u2,u3);
position += pos;";
                        break;
                    default: out_source += @""; break;
                }
                if(ApplySpeed)
                {
                    out_source += @"
velocity += normalize(pos - Sphere_center) * Speed;
";
                }

                return out_source;
            }
        }

        
    }
}
