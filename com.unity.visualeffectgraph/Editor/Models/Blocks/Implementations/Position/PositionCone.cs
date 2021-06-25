using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Position", variantProvider = typeof(PositionBaseProvider))]
    class PositionCone : PositionBase
    {
        public enum HeightMode
        {
            Base,
            Volume
        }

        [VFXSetting, Tooltip("Controls whether particles are spawned on the base of the cone, or throughout the entire volume.")]
        public HeightMode heightMode = HeightMode.Volume;

        public override string name { get { return string.Format(base.name, "Arc Cone"); } }
        protected override float thicknessDimensions { get { return 2.0f; } }

        public class InputProperties
        {
            [Tooltip("Sets the cone used for positioning the particles.")]
            public TArcCone arcCone = TArcCone.defaultValue;
        }

        public class CustomProperties
        {
            [Range(0, 1), Tooltip("Sets the position along the height to emit particles from when ‘Custom Emission’ is used.")]
            public float heightSequencer = 0.0f;
            [Range(0, 1), Tooltip("Sets the position on the arc to emit particles from when ‘Custom Emission’ is used.")]
            public float arcSequencer = 0.0f;
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var properties = PropertiesFromType(nameof(InputProperties));

                if (supportsVolumeSpawning)
                {
                    if (positionMode == PositionMode.ThicknessAbsolute || positionMode == PositionMode.ThicknessRelative)
                        properties = properties.Concat(PropertiesFromType(nameof(ThicknessProperties)));
                }

                if (spawnMode == SpawnMode.Custom)
                {
                    var customProperties = PropertiesFromType(nameof(CustomProperties));
                    if (heightMode == HeightMode.Base)
                        customProperties = customProperties.Where(o => o.property.name !=  nameof(CustomProperties.heightSequencer));

                    properties = properties.Concat(customProperties);
                }

                return properties;
            }
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                var allSlots = GetExpressionsFromSlots(this);

                foreach (var p in allSlots.Where(o => o.name != nameof(ThicknessProperties.Thickness)))
                    yield return p;

                var radius0 = allSlots.First(e => e.name == "arcCone_cone_radius0").exp;
                var radius1 = allSlots.First(e => e.name == "arcCone_cone_radius1").exp;
                var height = allSlots.First(e => e.name == "arcCone_cone_height").exp;
                var transform = allSlots.First(e => e.name == "arcCone_cone_transform").exp;

                var tanSlope = (radius1 - radius0) / height;
                var slope = new VFXExpressionATan(tanSlope);

                var thickness = allSlots.Where(o => o.name == nameof(ThicknessProperties.Thickness)).FirstOrDefault();
                yield return new VFXNamedExpression(CalculateVolumeFactor(positionMode, radius0, thickness.exp), "volumeFactor");

                yield return new VFXNamedExpression(new VFXExpressionCombine(new VFXExpression[] { new VFXExpressionSin(slope), new VFXExpressionCos(slope) }), "sincosSlope");

                var invFinalTransform = new VFXExpressionTransposeMatrix(new VFXExpressionInverseMatrix(transform));
                yield return new VFXNamedExpression(invFinalTransform, "arcCone_cone_inverseTranspose");
            }
        }

        protected override bool needDirectionWrite => true;

        public override string source
        {
            get
            {
                string outSource = "";

                if (spawnMode == SpawnMode.Random)
                    outSource += @"float theta = arcCone_arc * RAND;";
                else
                    outSource += @"float theta = arcCone_arc * arcSequencer;";

                outSource += @"
float rNorm = sqrt(volumeFactor + (1 - volumeFactor) * RAND);

float2 sincosTheta;
sincos(theta, sincosTheta.x, sincosTheta.y);
float2 pos = (sincosTheta * rNorm);
";

                if (heightMode == HeightMode.Base)
                {
                    outSource += @"
float hNorm = 0.0f;
";
                }
                else if (spawnMode == SpawnMode.Random)
                {
                    float distributionExponent = positionMode == PositionMode.Surface ? 2.0f : 3.0f;
                    outSource += $@"
float hNorm = 0.0f;
if (abs(arcCone_cone_radius0 - arcCone_cone_radius1) > VFX_EPSILON)
{{
    // Uniform distribution on cone
    float heightFactor = arcCone_cone_radius0 / max(VFX_EPSILON, arcCone_cone_radius1);
    float heightFactorPow = pow(heightFactor, {distributionExponent});
    hNorm = pow(heightFactorPow + (1.0f - heightFactorPow) * RAND, rcp({distributionExponent}));
    hNorm = (hNorm - heightFactor) / (1.0f - heightFactor); // remap on [0,1]
}}
else
    hNorm = RAND; // Uniform distribution on cylinder
";
                }
                else
                {
                    outSource += @"
float hNorm = heightSequencer;
";
                }

                outSource += @"
float3 finalPos = lerp(float3(pos * arcCone_cone_radius0, 0.0f), float3(pos * arcCone_cone_radius1, arcCone_cone_height), hNorm);
float3 finalDir = normalize(float3(pos * sincosSlope.x, sincosSlope.y));
finalPos = mul(arcCone_cone_transform, float4(finalPos.xzy, 1.0f)).xyz;
finalDir = mul(arcCone_cone_inverseTranspose, float4(finalDir.xzy, 0.0f)).xyz;
finalDir = normalize(finalDir);
";
                outSource += VFXBlockUtility.GetComposeString(compositionDirection, "direction", "finalDir", "blendDirection") + "\n";
                outSource += VFXBlockUtility.GetComposeString(compositionPosition, "position", "finalPos", "blendPosition") + "\n";

                return outSource;
            }
        }
    }
}
