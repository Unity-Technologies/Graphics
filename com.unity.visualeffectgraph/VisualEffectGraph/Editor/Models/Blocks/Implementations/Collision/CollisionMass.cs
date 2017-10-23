using System;
using System.Linq;
using System.Collections.Generic;

namespace UnityEditor.VFX.BlockLibrary
{
    [VFXInfo(category = "Collision")]
    class CollisionMass : VFXBlock
    {
        public override string name { get { return "Calculate Particle Mass"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kInitAndUpdateAndOutput; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }
        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Size, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(new VFXAttribute("mass", VFXValue.Constant(1.0f)), VFXAttributeMode.Write);
            }
        }

        public class InputProperties
        {
            public float Density = 1.0f;
        }

        public override string source
        {
            get
            {
                return @"
float radius = size.x * 0.5f;
float radiusCubed = radius * radius * radius;
mass = (4.0f / 3.0f) * UNITY_PI * radiusCubed * Density;
";
            }
        }
    }
}
