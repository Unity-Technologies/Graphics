using System;
using System.Collections.Generic;
using System.Linq;

using UnityEditor.VFX.Block;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    class NoiseSubVariantProvider : VariantProvider
    {
        private readonly NoiseBase.NoiseType m_MainVariantNoiseType;
        private readonly Noise.DimensionCount m_MainVariantDimensionCount;

        public NoiseSubVariantProvider(NoiseBase.NoiseType mainVariantNoiseType, Noise.DimensionCount mainVariantDimensionCount)
        {
            m_MainVariantNoiseType = mainVariantNoiseType;
            m_MainVariantDimensionCount = mainVariantDimensionCount;
        }

        public override IEnumerable<Variant> GetVariants()
        {
            foreach (NoiseBase.NoiseType type in Enum.GetValues(typeof(NoiseBase.NoiseType)))
            {
                var category = VFXLibraryStringHelper.Separator(type.ToString(), 0);
                foreach (Noise.DimensionCount dimension in Enum.GetValues(typeof(Noise.DimensionCount)))
                {
                    if (type == m_MainVariantNoiseType && dimension == m_MainVariantDimensionCount)
                        continue;
                    yield return new Variant(
                        type.ToString().Label().AppendLiteral("Noise").AppendLabel(VFXBlockUtility.GetNameString(dimension)),
                        category,
                        typeof(Noise),
                        new[]
                        {
                            new KeyValuePair<string, object>("type", type),
                            new KeyValuePair<string, object>("dimensions", dimension)
                        });
                }
            }
        }
    }

    class NoiseVariantProvider : VariantProvider
    {
        public override IEnumerable<Variant> GetVariants()
        {
            var mainNoiseType = Noise.NoiseType.Perlin;
            var mainNoiseDimension = Noise.DimensionCount.Three;
            yield return new Variant(
                mainNoiseType.ToString().Label().AppendLiteral("Noise").AppendLabel(VFXBlockUtility.GetNameString(mainNoiseDimension), false),
                $"Noise",
                typeof(Noise),
                new[]
                {
                    new KeyValuePair<string, object>("type", mainNoiseType),
                    new KeyValuePair<string, object>("dimensions", mainNoiseDimension)
                },
    () => new NoiseSubVariantProvider(mainNoiseType, mainNoiseDimension));
        }
    }

    [VFXHelpURL("Operator-CellularNoise")]
    [VFXInfo(variantProvider = typeof(NoiseVariantProvider))]
    class Noise : NoiseBase
    {
        public class InputPropertiesRange
        {
            [Tooltip("Sets the range within which the noise is calculated.")]
            public Vector2 range = new Vector2(-1.0f, 1.0f);
        }

        public class OutputPropertiesCommon
        {
            [Tooltip("Outputs the calculated noise value.")]
            public float Noise = 0.0f;
        }

        public class OutputProperties1D
        {
            [Tooltip("Outputs the rate of change of the noise.")]
            public float Derivatives = 0.0f;
        }

        public class OutputProperties2D
        {
            [Tooltip("Outputs the rate of change of the noise.")]
            public Vector2 Derivatives = Vector2.zero;
        }

        public class OutputProperties3D
        {
            [Tooltip("Outputs the rate of change of the noise.")]
            public Vector3 Derivatives = Vector3.zero;
        }

        public enum DimensionCount
        {
            One,
            Two,
            Three
        }

        [VFXSetting, Tooltip("Specifies whether the noise is output in one, two, or three dimensions.")]
        public DimensionCount dimensions = DimensionCount.Two;

        public override string name => type.ToString().Label(false).AppendLiteral("Noise").AppendLabel( VFXBlockUtility.GetNameString(dimensions));

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                IEnumerable<VFXPropertyWithValue> properties = null;

                if (dimensions == DimensionCount.One)
                    properties = PropertiesFromType(nameof(InputProperties1D));
                else if (dimensions == DimensionCount.Two)
                    properties = PropertiesFromType(nameof(InputProperties2D));
                else
                    properties = PropertiesFromType(nameof(InputProperties3D));

                properties = properties.Concat(PropertiesFromType(nameof(InputPropertiesCommon)));
                properties = properties.Concat(PropertiesFromType(nameof(InputPropertiesRange)));

                return properties;
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> outputProperties
        {
            get
            {
                IEnumerable<VFXPropertyWithValue> properties = PropertiesFromType(nameof(OutputPropertiesCommon));
                if (dimensions == DimensionCount.One)
                    properties = properties.Concat(PropertiesFromType(nameof(OutputProperties1D)));
                else if (dimensions == DimensionCount.Two)
                    properties = properties.Concat(PropertiesFromType(nameof(OutputProperties2D)));
                else
                    properties = properties.Concat(PropertiesFromType(nameof(OutputProperties3D)));

                return properties;
            }
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression parameters = new VFXExpressionCombine(inputExpression[1], inputExpression[3], inputExpression[4]);
            VFXExpression rangeMultiplier = (inputExpression[5].y - inputExpression[5].x);

            VFXExpression result;
            VFXExpression rangeMin = VFXValue.Constant(0.0f);
            VFXExpression rangeMax = VFXValue.Constant(1.0f);

            if (dimensions == DimensionCount.One)
            {
                if (type == NoiseType.Value)
                {
                    result = new VFXExpressionValueNoise1D(inputExpression[0], parameters, inputExpression[2]);
                }
                else if (type == NoiseType.Perlin)
                {
                    result = new VFXExpressionPerlinNoise1D(inputExpression[0], parameters, inputExpression[2]);
                    rangeMin = VFXValue.Constant(-1.0f);
                }
                else
                {
                    result = new VFXExpressionCellularNoise1D(inputExpression[0], parameters, inputExpression[2]);
                }

                VFXExpression x = VFXOperatorUtility.Fit(result.x, rangeMin, rangeMax, inputExpression[5].x, inputExpression[5].y);
                VFXExpression y = result.y * rangeMultiplier;
                return new[] { x, y };
            }
            else if (dimensions == DimensionCount.Two)
            {
                if (type == NoiseType.Value)
                {
                    result = new VFXExpressionValueNoise2D(inputExpression[0], parameters, inputExpression[2]);
                }
                else if (type == NoiseType.Perlin)
                {
                    result = new VFXExpressionPerlinNoise2D(inputExpression[0], parameters, inputExpression[2]);
                    rangeMin = VFXValue.Constant(-1.0f);
                }
                else
                {
                    result = new VFXExpressionCellularNoise2D(inputExpression[0], parameters, inputExpression[2]);
                }

                VFXExpression x = VFXOperatorUtility.Fit(result.x, rangeMin, rangeMax, inputExpression[5].x, inputExpression[5].y);
                VFXExpression y = result.y * rangeMultiplier;
                VFXExpression z = result.z * rangeMultiplier;
                return new[] { x, new VFXExpressionCombine(y, z) };
            }
            else
            {
                if (type == NoiseType.Value)
                {
                    result = new VFXExpressionValueNoise3D(inputExpression[0], parameters, inputExpression[2]);
                }
                else if (type == NoiseType.Perlin)
                {
                    result = new VFXExpressionPerlinNoise3D(inputExpression[0], parameters, inputExpression[2]);
                    rangeMin = VFXValue.Constant(-1.0f);
                }
                else
                {
                    result = new VFXExpressionCellularNoise3D(inputExpression[0], parameters, inputExpression[2]);
                }

                VFXExpression x = VFXOperatorUtility.Fit(result.x, rangeMin, rangeMax, inputExpression[5].x, inputExpression[5].y);
                VFXExpression y = result.y * rangeMultiplier;
                VFXExpression z = result.z * rangeMultiplier;
                VFXExpression w = result.w * rangeMultiplier;
                return new[] { x, new VFXExpressionCombine(y, z, w) };
            }
        }
    }
}
