using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Block
{
    class PositionSphere : PositionShapeBase
    {
        public class InputProperties
        {
            [Tooltip("Sets the sphere used for positioning the particles.")]
            public TArcSphere arcSphere = TArcSphere.defaultValue;
        }

        public class CustomProperties
        {
            [Range(0, 1), Tooltip("Sets the position along the height to emit particles from when ‘Custom Emission’ is used.")]
            public float heightSequencer = 0.0f;
            [Range(0, 1), Tooltip("When using customized emission, control the position around the arc to emit particles from.")]
            public float arcSequencer = 0.0f;
        }


        public override IEnumerable<VFXNamedExpression> GetParameters(PositionShape positionBase, List<VFXNamedExpression> allSlots)
        {
            VFXExpression transform = null;
            VFXExpression radius = null;
            VFXExpression thickness = null;

            foreach (var slot in allSlots)
            {
                if (slot.name == "arcSphere_arc"
                    || slot.name == "arcSequencer"
                    || slot.name == "heightSequencer")
                    yield return slot;

                if (slot.name == "arcSphere_sphere_transform")
                    transform = slot.exp;

                if (slot.name == "arcSphere_sphere_radius")
                    radius = slot.exp;

                if (slot.name == "Thickness")
                    thickness = slot.exp;
            }

            var radiusScale = VFXOperatorUtility.UniformScaleMatrix(radius);
            var finalTransform = new VFXExpressionTransformMatrix(transform, radiusScale);
            var inverseTransposeTRS = VFXOperatorUtility.InverseTransposeTRS(transform);
            yield return new VFXNamedExpression(finalTransform, "transform");
            yield return new VFXNamedExpression(inverseTransposeTRS, "inverseTranspose");
            yield return new VFXNamedExpression(CalculateVolumeFactor(positionBase.positionMode, radius, thickness, 3.0f), "volumeFactor");
        }

        public override string GetSource(PositionShape positionBase)
        {
            var outSource = string.Empty;
            if (positionBase.spawnMode == PositionShape.SpawnMode.Random)
            {
                outSource += @"float cosPhi = 2.0f * RAND - 1.0f;";
                outSource += @"float theta = arcSphere_arc * RAND;";
            }
            else
            {

                outSource += @"float cosPhi = 2.0f * heightSequencer - 1.0f;";
                outSource += @"float theta = arcSphere_arc * arcSequencer;";
            }

            outSource += @"
float rNorm = pow(abs(volumeFactor + (1 - volumeFactor) * RAND), 1.0f / 3.0f);
float2 sincosTheta;
sincos(theta, sincosTheta.x, sincosTheta.y);
float sinPhi = sqrt(1.0f - cosPhi * cosPhi);
sincosTheta *= sinPhi;
float3 currentAxisY = float3(sincosTheta, cosPhi);
float3 finalPos = float3(sincosTheta, cosPhi) * rNorm;
finalPos = mul(transform, float4(finalPos, 1.0f)).xyz;

float3 currentAxisZ = float3(-sincosTheta.y, sincosTheta.x, 0);
if (abs(cosPhi) > 0.99999f) currentAxisZ = float3(sign(cosPhi), 0, 0);

currentAxisY = mul(inverseTranspose, float4(currentAxisY, 0.0f)).xyz;
currentAxisZ = mul(inverseTranspose, float4(currentAxisZ, 0.0f)).xyz;
currentAxisY = normalize(currentAxisY);
currentAxisZ = normalize(currentAxisZ);

float3 currentAxisX = cross(currentAxisY, currentAxisZ);
";
            outSource += string.Format(positionBase.composePositionFormatString, "finalPos");

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
