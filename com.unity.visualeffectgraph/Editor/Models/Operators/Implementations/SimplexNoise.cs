using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Noise")]
    class SimplexNoise : NoiseBase
    {
        override public string name { get { return "Simplex Noise"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression parameters = new VFXExpressionCombine(inputExpression[1], inputExpression[2], inputExpression[4]);

            if (dimensions == DimensionCount.One)
                return new[] { new VFXExpressionSimplexNoise1D(inputExpression[0], parameters, inputExpression[3]) };
            else if (dimensions == DimensionCount.Two)
                return new[] { new VFXExpressionSimplexNoise2D(inputExpression[0], parameters, inputExpression[3]) };
            else
                return new[] { new VFXExpressionSimplexNoise3D(inputExpression[0], parameters, inputExpression[3]) };
        }
    }
}
