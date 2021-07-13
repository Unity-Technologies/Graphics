using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Implicit")]
    class EulerIntegration : VFXBlock
    {
        public override string name { get { return "Integration: Update Position"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.Update; } }
        public override VFXDataType compatibleData { get { return VFXDataType.Particle; } }
        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.ReadWrite);
                yield return new VFXAttributeInfo(VFXAttribute.Velocity, VFXAttributeMode.Read);
            }
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                yield return new VFXNamedExpression(VFXBuiltInExpression.DeltaTime, "deltaTime");
            }
        }

        public override string source
        {
            get
            {
                return "position += velocity * deltaTime;";
            }
        }
    }
}
