using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Noise")]
    class ValueNoise : NoiseBase
    {
        override public string name { get { return "Value Noise"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression parameters = new VFXExpressionCombine(inputExpression[1], inputExpression[2], inputExpression[4]);

            if (dimensions == DimensionCount.One)
                return new[] { new VFXExpressionValueNoise1D(inputExpression[0], parameters, inputExpression[3]) };
            else if (dimensions == DimensionCount.Two)
                return new[] { new VFXExpressionValueNoise2D(inputExpression[0], parameters, inputExpression[3]) };
            else
                return new[] { new VFXExpressionValueNoise3D(inputExpression[0], parameters, inputExpression[3]) };
        }
    }
}
