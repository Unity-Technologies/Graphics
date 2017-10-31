using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Force")]
    class Drag : VFXBlock
    {
        [VFXSetting]
        public bool UseParticleSize = false;

        public override string name { get { return "Linear Drag"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kUpdate; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                foreach (var p in GetExpressionsFromSlots(this))
                    yield return p;

                yield return new VFXNamedExpression(VFXBuiltInExpression.DeltaTime, "deltaTime");
            }
        }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Velocity, VFXAttributeMode.ReadWrite);
                yield return new VFXAttributeInfo(VFXAttribute.Mass, VFXAttributeMode.Read);
                if (UseParticleSize) yield return new VFXAttributeInfo(VFXAttribute.Size, VFXAttributeMode.Read);
            }
        }

        public class InputProperties
        {
            [Tooltip("Drag coefficient of the particle")]
            public float dragCoefficient = 0.5f;
        }

        public override string source
        {
            get
            {
                string coefficient = "dragCoefficient";
                if (UseParticleSize)
                    coefficient = "(dragCoefficient * size.x * size.y)";

                return string.Format("velocity *= max(0.0,(1.0 - ({0} * deltaTime) / mass));", coefficient);
            }
        }
    }
}
