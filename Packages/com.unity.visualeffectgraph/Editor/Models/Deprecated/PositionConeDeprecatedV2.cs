using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    class PositionConeDeprecatedV2 : PositionBase
    {
        public override void Sanitize(int version)
        {
            var newPositionShape = ScriptableObject.CreateInstance<PositionShape>();
            SanitizeHelper.MigrateBlockPositionToComposed(GetGraph(), GetParent().position, newPositionShape, this, PositionShapeBase.Type.Cone);
            ReplaceModel(newPositionShape, this);
        }


        [VFXSetting, Tooltip("Controls whether particles are spawned on the base of the cone, or throughout the entire volume.")]
        public HeightMode heightMode = HeightMode.Volume;

        public override string name { get { return string.Format(base.name, "Arc Cone"); } }

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
if (abs(arcCone_cone_baseRadius - arcCone_cone_topRadius) > VFX_EPSILON)
{{
    // Uniform distribution on cone
    float heightFactor = arcCone_cone_baseRadius / max(VFX_EPSILON, arcCone_cone_topRadius);
    float heightFactorPow = pow(heightFactor, {distributionExponent});
    hNorm = pow(abs(heightFactorPow + (1.0f - heightFactorPow) * RAND), rcp({distributionExponent}));
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
float3 finalPos = lerp(float3(pos * arcCone_cone_baseRadius, 0.0f), float3(pos * arcCone_cone_topRadius, arcCone_cone_height), hNorm);
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
