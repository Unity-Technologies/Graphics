using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
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
            [Tooltip("Particle density, measured in kg/dm^3")]
            public float Density = 1.0f;
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                yield return new VFXNamedExpression(inputSlots[0].GetExpression() * VFXValue.Constant(1000.0f), "Density");
            }
        }

        public override string source
        {
            get
            {
                return @"
float xy = size.x * size.y;
float z = (size.x + size.y) * 0.5f;
float radiusCubed = xy * z * 0.125f;
mass = (4.0f / 3.0f) * UNITY_PI * radiusCubed * Density;
";
            }
        }
    }
}
