using System;
using System.Linq;
using System.Collections.Generic;

namespace UnityEditor.VFX.BlockLibrary
{
    [VFXInfo(category = "Attribute")]
    class AttributeMassFromVolume : VFXBlock
    {
        public override string name { get { return "Calculate Mass From Volume"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kInitAndUpdate; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }
        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Size, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Mass, VFXAttributeMode.Write);
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
float xy = size.x * size.y;
float z = (size.x + size.y) * 0.5f;
float radiusCubed = xy * z * 0.5f;
mass = (4.0f / 3.0f) * UNITY_PI * radiusCubed * Density;
";
            }
        }
    }
}
