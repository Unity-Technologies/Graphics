using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Noise", variantProvider = typeof(NoiseVariantProvider))]
    class ValueNoise : NoiseBase
    {
        override protected string noiseName { get { return "Value"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression parameters = new VFXExpressionCombine(inputExpression[1], inputExpression[2], inputExpression[4]);

            if (type == NoiseType.Curl)
            {
                if (curlDimensions == CurlDimensionCount.Two)
                {
                    return new[] { new VFXExpressionValueCurlNoise2D(inputExpression[0], parameters, inputExpression[3]) };
                }
                else
                {
                    return new[] { new VFXExpressionValueCurlNoise3D(inputExpression[0], parameters, inputExpression[3]) };
                }
            }
            else
            {
                if (dimensions == DimensionCount.One)
                {
                    VFXExpression noise = new VFXExpressionValueNoise1D(inputExpression[0], parameters, inputExpression[3]);
                    noise = VFXOperatorUtility.Fit(noise, VFXValue.Constant(new Vector2(-1, -1)), VFXValue.Constant(Vector2.one), VFXOperatorUtility.CastFloat(inputExpression[5].x, noise.valueType), VFXOperatorUtility.CastFloat(inputExpression[5].y, noise.valueType));
                    return new[] { noise.x, noise.y };
                }
                else if (dimensions == DimensionCount.Two)
                {
                    VFXExpression noise = new VFXExpressionValueNoise2D(inputExpression[0], parameters, inputExpression[3]);
                    noise = VFXOperatorUtility.Fit(noise, VFXValue.Constant(new Vector3(-1, -1, -1)), VFXValue.Constant(Vector3.one), VFXOperatorUtility.CastFloat(inputExpression[5].x, noise.valueType), VFXOperatorUtility.CastFloat(inputExpression[5].y, noise.valueType));
                    return new[] { noise.x, new VFXExpressionCombine(noise.y, noise.z) };
                }
                else
                {
                    VFXExpression noise = new VFXExpressionValueNoise3D(inputExpression[0], parameters, inputExpression[3]);
                    noise = VFXOperatorUtility.Fit(noise, VFXValue.Constant(new Vector4(-1, -1, -1, -1)), VFXValue.Constant(Vector4.one), VFXOperatorUtility.CastFloat(inputExpression[5].x, noise.valueType), VFXOperatorUtility.CastFloat(inputExpression[5].y, noise.valueType));
                    return new[] { noise.x, new VFXExpressionCombine(noise.y, noise.z, noise.w) };
                }
            }
        }
    }
}
