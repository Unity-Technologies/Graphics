using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using static UnityEditor.VFX.Block.PositionBase;

namespace UnityEditor.VFX.Block
{
    sealed class PositionCone : PositionShapeBase
    {
        public override bool hasBase => true;

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

        public override IEnumerable<VFXNamedExpression> GetParameters(PositionShape positionBase, List<VFXNamedExpression> allSlots)
        {
            VFXExpression baseRadius = null;
            VFXExpression topRadius = null;
            VFXExpression height = null;
            VFXExpression transform = null;
            VFXExpression thickness = null;

            foreach (var slot in allSlots)
            {
                if (slot.name.StartsWith("arcCone")
                    || slot.name == nameof(CustomProperties.arcSequencer)
                    || slot.name == nameof(CustomProperties.heightSequencer))
                    yield return slot;

                if (slot.name == "arcCone_cone_baseRadius")
                    baseRadius = slot.exp;
                else if (slot.name == "arcCone_cone_topRadius")
                    topRadius = slot.exp;
                else if (slot.name == "arcCone_cone_height")
                    height = slot.exp;
                else if (slot.name == "arcCone_cone_transform")
                    transform = slot.exp;
                else if (slot.name == nameof(PositionShape.ThicknessProperties.Thickness))
                    thickness = slot.exp;
            }

            var tanSlope = (topRadius - baseRadius) / height;
            var slope = new VFXExpressionATan(tanSlope);
            yield return new VFXNamedExpression(CalculateVolumeFactor(positionBase.positionMode, baseRadius, thickness, 2.0f), "volumeFactor");
            yield return new VFXNamedExpression(new VFXExpressionCombine(new VFXExpression[] { new VFXExpressionSin(slope), new VFXExpressionCos(slope) }), "sincosSlope");
        }

        public override string GetSource(PositionShape positionBase)
        {
            string outSource = "";

            if (positionBase.spawnMode == SpawnMode.Random)
                outSource += @"float theta = arcCone_arc * RAND;";
            else
                outSource += @"float theta = arcCone_arc * arcSequencer;";

            outSource += @"
float rNorm = sqrt(volumeFactor + (1 - volumeFactor) * RAND);

float2 sincosTheta;
sincos(theta, sincosTheta.x, sincosTheta.y);
float2 pos = (sincosTheta * rNorm);
";

            if (positionBase.heightMode == HeightMode.Base)
            {
                outSource += @"
float hNorm = 0.0f;
";
            }
            else if (positionBase.spawnMode == SpawnMode.Random)
            {
                float distributionExponent = positionBase.positionMode == PositionMode.Surface ? 2.0f : 3.0f;
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
float3 currentAxisZ = float3(sincosTheta * sincosSlope.x, sincosSlope.y);
float3 currentAxisY = float3(sincosTheta, -sincosSlope.x);

finalPos = mul(arcCone_cone_transform, float4(finalPos.xzy, 1.0f)).xyz;
currentAxisY = mul(arcCone_cone_transform, float4(currentAxisY.xzy, 0.0f)).xyz;
currentAxisZ = mul(arcCone_cone_transform, float4(currentAxisZ.xzy, 0.0f)).xyz;
currentAxisY = normalize(currentAxisY);
currentAxisZ = normalize(currentAxisZ);
float3 currentAxisX = cross(currentAxisY, currentAxisZ);
";
            if (positionBase.applyOrientation.HasFlag(PositionBase.Orientation.Axes))
            {
                outSource += VFXBlockUtility.GetComposeString(positionBase.compositionAxes, "axisX", "currentAxisX", "blendAxes") + "\n";
                outSource += VFXBlockUtility.GetComposeString(positionBase.compositionAxes, "axisY", "currentAxisY", "blendAxes") + "\n";
                outSource += VFXBlockUtility.GetComposeString(positionBase.compositionAxes, "axisZ", "currentAxisZ", "blendAxes") + "\n";
            }

            if (positionBase.applyOrientation.HasFlag(PositionBase.Orientation.Direction))
            {
                outSource += string.Format(positionBase.composeDirectionFormatString, "currentAxisZ");
            }

            outSource += VFXBlockUtility.GetComposeString(positionBase.compositionPosition, "position", "finalPos", "blendPosition") + "\n";

            return outSource;
        }
    }
}
