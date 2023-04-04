using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXHelpURL("Block-Gravity")]
    [VFXInfo(category = "Force")]
    class Gravity : VFXBlock
    {
        public override string name { get { return "Gravity"; } }
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
            }
        }

        public class InputProperties
        {
            [Tooltip("Sets the gravity acting upon the particles.")]
            public Vector Force = new Vector3(0.0f, -9.81f, 0.0f);
        }

        public override string source
        {
            get
            {
                return "velocity += Force * deltaTime;";
            }
        }
    }
}
