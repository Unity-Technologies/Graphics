using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Force")]
    class Drag : VFXBlock
    {
        [VFXSetting, Tooltip("When enabled, the particle size will affect the drag. Larger particles have a higher linear drag.")]
        public bool UseParticleSize = false;

        public override string name { get { return "Linear Drag"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.Update; } }
        public override VFXDataType compatibleData { get { return VFXDataType.Particle; } }

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
                if (UseParticleSize)
                {
                    yield return new VFXAttributeInfo(VFXAttribute.Size, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.ScaleX, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.ScaleY, VFXAttributeMode.Read);
                }
            }
        }

        public class InputProperties
        {
            [Tooltip("Sets the drag coefficient. Higher drag forces particles to slow down more.")]
            public float dragCoefficient = 0.5f;
        }

        public override string source
        {
            get
            {
                string source = string.Empty;
                if (UseParticleSize)
                {
                    source = @"
float2 side = size * float2(scaleX, scaleY);
dragCoefficient *= side.x * side.y;
";
                }

                return source + "velocity *= max(0.0,(1.0 - (dragCoefficient * deltaTime) / mass));";
            }
        }
    }
}
