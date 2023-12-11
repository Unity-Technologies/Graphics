using System.Collections.Generic;
using UnityEditor.Graphs;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Block
{
    sealed class PositionCircle : PositionShapeBase
    {
        public class InputProperties
        {
            [Tooltip("Sets the circle used for positioning the particles.")]
            public TArcCircle arcCircle = TArcCircle.defaultValue;
        }

        public class CustomProperties
        {
            [Range(0, 1), Tooltip("Sets the position on the arc to emit particles from when ‘Custom Emission’ is used.")]
            public float arcSequencer = 0.0f;
        }

        public override IEnumerable<VFXNamedExpression> GetParameters(PositionShape positionBase, List<VFXNamedExpression> allSlots)
        {
            VFXExpression arcCircle_arc = null;
            VFXExpression arcCircleRadius = null;
            VFXExpression arcCircle_transform = null;
            VFXExpression arcSequencer = null;
            VFXExpression thickness = null;

            foreach (var slot in allSlots)
            {
                if (slot.name == "arcCircle_arc")
                    arcCircle_arc = slot.exp;
                else if (slot.name == "arcCircle_circle_radius")
                    arcCircleRadius = slot.exp;
                else if (slot.name == "arcSequencer")
                    arcSequencer = slot.exp;
                else if (slot.name == "arcCircle_circle_transform")
                    arcCircle_transform = slot.exp;
                else if (slot.name == nameof(PositionBase.ThicknessProperties.Thickness))
                    thickness = slot.exp;
            }

            VFXExpression theta;
            if (positionBase.spawnMode == PositionBase.SpawnMode.Random)
                theta = arcCircle_arc * new VFXExpressionRandom(true, new RandId(this, 0));
            else
                theta = arcCircle_arc * arcSequencer;

            var one = VFXOperatorUtility.OneExpression[VFXValueType.Float];

            var volumeFactor = CalculateVolumeFactor(positionBase.positionMode, arcCircleRadius, thickness, 2.0f);
            var rNorm = VFXOperatorUtility.Sqrt(volumeFactor + (one - volumeFactor) * new VFXExpressionRandom(true, new RandId(this, 1)));
            var sinTheta = new VFXExpressionSin(theta);
            var cosTheta = new VFXExpressionCos(theta);

            yield return new VFXNamedExpression(rNorm, "rNorm");
            yield return new VFXNamedExpression(sinTheta, "sinTheta");
            yield return new VFXNamedExpression(cosTheta, "cosTheta");

            var radiusScale = VFXOperatorUtility.UniformScaleMatrix(arcCircleRadius);
            var finalTransform = new VFXExpressionTransformMatrix(arcCircle_transform, radiusScale);

            var invFinalTransform = VFXOperatorUtility.InverseTransposeTRS(arcCircle_transform);
            yield return new VFXNamedExpression(finalTransform, "transform");
            yield return new VFXNamedExpression(invFinalTransform, "inverseTranspose");
        }

        public override string GetSource(PositionShape positionBase)
        {
            var outSource = @"
float3 currentAxisY = float3(sinTheta, cosTheta, 0.0f);
float3 finalPos = float3(sinTheta, cosTheta, 0.0f) * rNorm;
finalPos = mul(transform, float4(finalPos, 1.0f)).xyz;
currentAxisY = mul(inverseTranspose, float4(currentAxisY, 0.0f)).xyz;
currentAxisY = normalize(currentAxisY);
float3 currentAxisZ = mul(inverseTranspose, float4(0.0f, 0.0f, 1.0f, 0.0f)).xyz;
currentAxisZ = normalize(currentAxisZ);
float3 currentAxisX = cross(currentAxisY, currentAxisZ);
";
            outSource += string.Format(positionBase.composePositionFormatString, "finalPos") + "\n";

            if (positionBase.applyOrientation.HasFlag(PositionBase.Orientation.Axes))
            {
                outSource += VFXBlockUtility.GetComposeString(positionBase.compositionAxes, "axisX", "currentAxisX", "blendAxes") + "\n";
                outSource += VFXBlockUtility.GetComposeString(positionBase.compositionAxes, "axisY", "currentAxisY", "blendAxes") + "\n";
                outSource += VFXBlockUtility.GetComposeString(positionBase.compositionAxes, "axisZ", "currentAxisZ", "blendAxes") + "\n";
            }

            if (positionBase.applyOrientation.HasFlag(PositionBase.Orientation.Direction))
            {
                outSource += string.Format(positionBase.composeDirectionFormatString, "currentAxisY") + "\n";
            }

            return outSource;
        }

    }
}
