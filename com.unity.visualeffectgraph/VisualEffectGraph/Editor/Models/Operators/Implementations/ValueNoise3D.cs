using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Noise")]
    class ValueNoise3D : NoiseBase
    {
        public class InputProperties
        {
            [Tooltip("The coordinate in the noise field to take the sample from.")]
            public Vector3 coordinate = Vector3.zero;
        }

        override public string name { get { return "Value Noise (3D)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionValueNoise3D(inputExpression[0], new VFXExpressionCombine(inputExpression[1], inputExpression[2], inputExpression[4]), inputExpression[3]) };
        }
    }
}
