using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Noise")]
    class PerlinNoise : NoiseBase
    {
        override public string name { get { return "Perlin Noise"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression parameters = new VFXExpressionCombine(inputExpression[1], inputExpression[2], inputExpression[4]);

            if (dimensions == DimensionCount.One)
                return new[] { new VFXExpressionPerlinNoise1D(inputExpression[0], parameters, inputExpression[3]) };
            else if (dimensions == DimensionCount.Two)
                return new[] { new VFXExpressionPerlinNoise2D(inputExpression[0], parameters, inputExpression[3]) };
            else
                return new[] { new VFXExpressionPerlinNoise3D(inputExpression[0], parameters, inputExpression[3]) };
        }
    }
}
