using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

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
        public HeightMode heightMode;

        public override string name { get { return string.Format(base.name, "Cone"); } }
        protected override float thicknessDimensions { get { return 2.0f; } }

        public class InputProperties
        {
            [Tooltip("Sets the cone used for positioning the particles.")]
            public ArcCone ArcCone = ArcCone.defaultValue;
        }

        public class CustomProperties
        {
            [Range(0, 1), Tooltip("Sets the position along the height to emit particles from when ‘Custom Emission’ is used.")]
            public float HeightSequencer = 0.0f;
            [Range(0, 1), Tooltip("Sets the position on the arc to emit particles from when ‘Custom Emission’ is used.")]
            public float ArcSequencer = 0.0f;
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var properties = PropertiesFromType("InputProperties");

                if (supportsVolumeSpawning)
                {
                    if (positionMode == PositionMode.ThicknessAbsolute || positionMode == PositionMode.ThicknessRelative)
                        properties = properties.Concat(PropertiesFromType("ThicknessProperties"));
                }

                if (spawnMode == SpawnMode.Custom)
                {
                    var customProperties = PropertiesFromType("CustomProperties");
                    if (heightMode == HeightMode.Base)
                        customProperties = customProperties.Where(o => o.property.name != "HeightSequencer");

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

                foreach (var p in allSlots.Where(e => e.name == "ArcCone_arc"
                                                   || e.name == "ArcCone_radius0"
                                                   || e.name == "ArcCone_radius1"
                                                   || e.name == "ArcCone_height"
                                                   || e.name == "ArcSequencer"
                                                   || e.name == "HeightSequencer"))
                    yield return p;

                VFXExpression radius0 = allSlots.First(e => e.name == "ArcCone_radius0").exp;
                VFXExpression radius1 = allSlots.First(e => e.name == "ArcCone_radius1").exp;
                VFXExpression height = allSlots.First(e => e.name == "ArcCone_height").exp;
                VFXExpression center = allSlots.First(e => e.name == "ArcCone_center").exp;
                VFXExpression eulerAngle = allSlots.First(e => e.name == "ArcCone_angles").exp;
                VFXExpression tanSlope = (radius1 - radius0) / height;
                VFXExpression slope = new VFXExpressionATan(tanSlope);

                var thickness = allSlots.Where(o => o.name == nameof(ThicknessProperties.Thickness)).FirstOrDefault();
                yield return new VFXNamedExpression(CalculateVolumeFactor(positionMode, radius0, thickness.exp), "volumeFactor");

                yield return new VFXNamedExpression(new VFXExpressionCombine(new VFXExpression[] { new VFXExpressionSin(slope), new VFXExpressionCos(slope) }), "sincosSlope");

                var zeroF3 = VFXOperatorUtility.ZeroExpression[VFXValueType.Float3];
                var oneF3 = VFXOperatorUtility.OneExpression[VFXValueType.Float3];

                //Warning : we can't manipulate eulerAngle result in input of VFXExpressionTRSToMatrix to keep possible reduction of VFXExpressionExtractAnglesFromMatrix
                VFXExpression rotationMatrix = new VFXExpressionTRSToMatrix(zeroF3, eulerAngle, oneF3);
                VFXExpression i = new VFXExpressionMatrixToVector3s(rotationMatrix, VFXValue.Constant(0));
                VFXExpression j = new VFXExpressionMatrixToVector3s(rotationMatrix, VFXValue.Constant(1));
                VFXExpression k = new VFXExpressionMatrixToVector3s(rotationMatrix, VFXValue.Constant(2));

                var transformMatrix = new VFXExpressionVector3sToMatrix(i, k, j, center); //Expected axis inversion
                yield return new VFXNamedExpression(transformMatrix, "transformMatrix");
            }
        }

        protected override bool needDirectionWrite => true;

        public override string source
        {
            get
            {
                string outSource = "";

                if (spawnMode == SpawnMode.Random)
                    outSource += @"float theta = ArcCone_arc * RAND;";
                else
                    outSource += @"float theta = ArcCone_arc * ArcSequencer;";

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
if (abs(ArcCone_radius0 - ArcCone_radius1) > VFX_EPSILON)
{{
    // Uniform distribution on cone
    float heightFactor = ArcCone_radius0 / max(VFX_EPSILON,ArcCone_radius1);
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
float hNorm = HeightSequencer;
";
                }

                outSource += @"
float3 finalPos = lerp(float3(pos * ArcCone_radius0, 0.0f), float3(pos * ArcCone_radius1, ArcCone_height), hNorm);
float3 finalDir = normalize(float3(pos * sincosSlope.x, sincosSlope.y));
finalPos = mul(transformMatrix, float4(finalPos, 1.0f)).xyz;
finalDir = mul((float3x3)transformMatrix, finalDir);
";
                outSource += VFXBlockUtility.GetComposeString(compositionDirection, "direction", "finalDir", "blendDirection") + "\n";
                outSource += VFXBlockUtility.GetComposeString(compositionPosition, "position", "finalPos", "blendPosition");

                return outSource;
            }
        }
    }
}
