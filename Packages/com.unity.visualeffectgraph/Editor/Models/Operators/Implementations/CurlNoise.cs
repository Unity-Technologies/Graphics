using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    class CurlNoiseVariantProvider : VariantProvider
    {
        protected sealed override Dictionary<string, object[]> variants { get; } = new Dictionary<string, object[]>
        {
            {
                "type",
                Enum.GetValues(typeof(NoiseBase.NoiseType)).OfType<NoiseBase.NoiseType>()
                    .Where(o => o != NoiseBase.NoiseType.Cellular)
                    .Cast<object>()
                    .ToArray()
            },
            {
                "dimensions",
                Enum.GetValues(typeof(CurlNoise.DimensionCount))
                    .Cast<object>()
                    .ToArray()
            }
        };
    }

    [VFXHelpURL("Operator-CellularCurlNoise")]
    [VFXInfo(category = "Noise", variantProvider = typeof(CurlNoiseVariantProvider))]
    class CurlNoise : NoiseBase
    {
        public class InputPropertiesAmplitude
        {
            [Tooltip("Sets the magnitude of the noise. Higher amplitudes result in a greater range of the noise value.")]
            public float amplitude = 1.0f;
        }

        public class OutputProperties2D
        {
            [Tooltip("Outputs the calculated noise vector.")]
            public Vector2 Noise = Vector2.zero;
        }

        public class OutputProperties3D
        {
            [Tooltip("Outputs the calculated noise vector.")]
            public Vector3 Noise = Vector3.zero;
        }

        public enum DimensionCount
        {
            Two,
            Three
        }

        [VFXSetting, Tooltip("Specifies whether the noise is output in one, two, or three dimensions.")]
        public DimensionCount dimensions = DimensionCount.Two;

        override public string name
        {
            get
            {
                return type.ToString() + " Curl Noise " + (((int)dimensions) + 2) + "D";
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                IEnumerable<VFXPropertyWithValue> properties = null;

                if (dimensions == DimensionCount.Two)
                    properties = PropertiesFromType(nameof(InputProperties2D));
                else
                    properties = PropertiesFromType(nameof(InputProperties3D));

                properties = properties.Concat(PropertiesFromType(nameof(InputPropertiesCommon)));
                properties = properties.Concat(PropertiesFromType(nameof(InputPropertiesAmplitude)));

                return properties;
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> outputProperties
        {
            get
            {
                if (dimensions == DimensionCount.Two)
                    return PropertiesFromType(nameof(OutputProperties2D));
                else
                    return PropertiesFromType(nameof(OutputProperties3D));
            }
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression parameters = new VFXExpressionCombine(inputExpression[1], inputExpression[3], inputExpression[4]);

            VFXExpression result;
            if (dimensions == DimensionCount.Two)
            {
                if (type == NoiseType.Value)
                    result = new VFXExpressionValueCurlNoise2D(inputExpression[0], parameters, inputExpression[2]);
                else if (type == NoiseType.Perlin)
                    result = new VFXExpressionPerlinCurlNoise2D(inputExpression[0], parameters, inputExpression[2]);
                else
                    result = new VFXExpressionCellularCurlNoise2D(inputExpression[0], parameters, inputExpression[2]);
            }
            else
            {
                if (type == NoiseType.Value)
                    result = new VFXExpressionValueCurlNoise3D(inputExpression[0], parameters, inputExpression[2]);
                else if (type == NoiseType.Perlin)
                    result = new VFXExpressionPerlinCurlNoise3D(inputExpression[0], parameters, inputExpression[2]);
                else
                    result = new VFXExpressionCellularCurlNoise3D(inputExpression[0], parameters, inputExpression[2]);
            }

            return new[] { result * VFXOperatorUtility.CastFloat(inputExpression[5], result.valueType) };
        }
    }
}
