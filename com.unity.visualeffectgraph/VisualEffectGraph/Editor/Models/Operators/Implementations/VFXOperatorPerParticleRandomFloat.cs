using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Random")]
    class VFXOperatorPerParticleRandomFloat : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("Minimum Range value")]
            public FloatN min = new FloatN(-1.0f);
            [Tooltip("Maximum Range value")]
            public FloatN max = new FloatN(1.0f);
        }

        public override string name
        {
            get
            {
                return "Random Float (Per-Particle)";
            }
        }
        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression[] output = new VFXExpression[] { VFXOperatorUtility.Lerp(inputExpression[0], inputExpression[1], new VFXAttributeExpression(VFXAttribute.Phase)) };
            return output;
        }
    }
}
