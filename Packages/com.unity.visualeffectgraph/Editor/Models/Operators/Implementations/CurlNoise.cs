using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.VFX.Block;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    class CurlNoiseSubVariantProvider : VariantProvider
    {
        private readonly NoiseBase.NoiseType m_MainVariantNoiseType;
        private readonly CurlNoise.DimensionCount m_MainVariantDimensionCount;

        public CurlNoiseSubVariantProvider(NoiseBase.NoiseType mainVariantNoiseType, CurlNoise.DimensionCount mainVariantDimensionCount)
        {
            m_MainVariantNoiseType = mainVariantNoiseType;
            m_MainVariantDimensionCount = mainVariantDimensionCount;
        }

        public override IEnumerable<Variant> GetVariants()
        {
            foreach (NoiseBase.NoiseType type in Enum.GetValues(typeof(NoiseBase.NoiseType)))
            {
                if (type == NoiseBase.NoiseType.Cellular)
                    continue;
                var category = VFXLibraryStringHelper.Separator(type.ToString(), 0);
                foreach (CurlNoise.DimensionCount dimension in Enum.GetValues(typeof(CurlNoise.DimensionCount)))
                {
                    if (type == m_MainVariantNoiseType && dimension == m_MainVariantDimensionCount)
                        continue;
                    yield return new Variant(
                        type.ToString().Label().AppendLiteral("Curl Noise").AppendLabel(VFXBlockUtility.GetNameString(dimension)),
                        category,
                        typeof(CurlNoise),
                        new[]
                        {
                            new KeyValuePair<string, object>("type", type),
                            new KeyValuePair<string, object>("dimensions", dimension)
                        });
                }
            }
        }
    }

    class CurlNoiseVariantProvider : VariantProvider
    {
        public override IEnumerable<Variant> GetVariants()
        {
            var mainNoiseType = Noise.NoiseType.Perlin;
            var mainNoiseDimension = CurlNoise.DimensionCount.Three;

            yield return new Variant(
                mainNoiseType.ToString().Label().AppendLiteral("Curl Noise").AppendLabel(VFXBlockUtility.GetNameString(mainNoiseDimension), false),
                "Noise",
                typeof(CurlNoise),
                new []
                {
                    new KeyValuePair<string, object>("type", mainNoiseType),
                    new KeyValuePair<string, object>("dimensions", mainNoiseDimension)
                },
                () => new CurlNoiseSubVariantProvider(mainNoiseType, mainNoiseDimension));
        }
    }

    [VFXHelpURL("Operator-CellularCurlNoise")]
    [VFXInfo(variantProvider = typeof(CurlNoiseVariantProvider))]
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

        public override string name => type.ToString().Label(false).AppendLiteral("Curl Noise").AppendLabel( VFXBlockUtility.GetNameString(dimensions));

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
