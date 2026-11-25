using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-SampleTerrain")]
    [VFXInfo(name = "Sample Terrain", category = "Sampling")]
    class SampleTerrain : VFXOperator
    {
        override public string name { get { return "Sample Terrain"; } }

        public enum SamplingType
        {
            UV = 0,
            Position = 1,
        }

        [VFXSetting, SerializeField, Tooltip("Specifies the input type for sampling.")]
        private SamplingType samplingType = SamplingType.UV;

        public class InputPropertiesTerrain
        {
            [Tooltip("The terrain to sample from.")]
            public TerrainType terrain = default;
        }

        public class InputPropertiesUv
        {
            [Tooltip("The texture coordinate used for sampling.")]
            public Vector2 UV = Vector2.zero;
        }

        public class InputPropertiesPosition
        {
            [Tooltip("The position used for sampling.")]
            public Position position;
        }

        protected sealed override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var props = base.inputProperties;

                props = props.Concat(PropertiesFromType(nameof(InputPropertiesTerrain)));

                if (samplingType == SamplingType.UV)
                    props = props.Concat(PropertiesFromType(nameof(InputPropertiesUv)));
                else if (samplingType == SamplingType.Position)
                    props = props.Concat(PropertiesFromType(nameof(InputPropertiesPosition)));

                return props;
            }
        }

        protected sealed override IEnumerable<VFXPropertyWithValue> outputProperties
        {
            get
            {
                yield return new VFXPropertyWithValue(
                    new VFXProperty(typeof(float), "Height (Normalized)",
                        new TooltipAttribute("The height value at the sampled position in the range 0 to 1.")));
                yield return new VFXPropertyWithValue(
                    new VFXProperty(typeof(float), "Height (World)",
                        new TooltipAttribute("The world space terrain height at the sampled position.")));
            }
        }

        // Force the operator space to world to make every spaceable input arrive already converted,
        // so the sample position and the terrain position are guaranteed to be comparable here.
        public override VFXSpace GetOutputSpaceFromSlot(VFXSlot slot)
        {
            return VFXSpace.World;
        }

        private static VFXExpression PositionToUv(VFXExpression sampleLocation, VFXExpression terrainPosition, VFXExpression terrainSize)
        {
            var terrainSpacePosition = sampleLocation - terrainPosition;
            var normalizedPosition = terrainSpacePosition / terrainSize;
            return new VFXExpressionCombine(normalizedPosition.x, normalizedPosition.z);
        }

        protected sealed override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            // Inputs
            var terrainPosition = inputExpression[0];
            var terrainSize = inputExpression[2];
            var heightmap = inputExpression[3];
            var maxHeight = inputExpression[4];
            var sampleLocation = inputExpression[5];

            // Constants
            var halfFloat2 = VFXOperatorUtility.HalfExpression[VFXValueType.Float2];
            var oneFloat2 = VFXOperatorUtility.OneExpression[VFXValueType.Float2];
            var oneUint = VFXOperatorUtility.OneExpression[VFXValueType.Uint32];
            var zero = VFXOperatorUtility.ZeroExpression[VFXValueType.Float];
            var two = VFXOperatorUtility.TwoExpression[VFXValueType.Float];

            // Transform position into UV
            if (samplingType == SamplingType.Position)
                sampleLocation = PositionToUv(sampleLocation, terrainPosition, terrainSize);

            // Support odd sized terrains in the event this gets added
            var width = new VFXExpressionMax(new VFXExpressionTextureWidth(heightmap), oneUint);
            var height = new VFXExpressionMax(new VFXExpressionTextureHeight(heightmap), oneUint);
            var dimensions = new VFXExpressionCombine(new VFXExpressionCastUintToFloat(width), new VFXExpressionCastUintToFloat(height));

            // Map from uv space to Power of 2 minus 1 (513x513 -> 512x512)
            var texelPosition = sampleLocation * (dimensions - oneFloat2);
            var remappedUV = (texelPosition + halfFloat2) / dimensions;

            VFXExpression heightmapSample = new VFXExpressionSampleTexture2D(heightmap, remappedUV, zero);
            VFXExpression heightmapValue = new VFXExpressionUnpackHeightmap(heightmapSample) * two;
            VFXExpression terrainHeight = (heightmapValue * maxHeight) + terrainPosition.y;

            return new[] { heightmapValue, terrainHeight };
        }
    }
}
