using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Block
{
    sealed class PositionTorus : PositionShapeBase
    {
        public class InputProperties
        {
            [Tooltip("Sets the torus used for positioning the particles.")]
            public TArcTorus arcTorus = TArcTorus.defaultValue;
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
            VFXExpression thickness = null;
            VFXExpression majorRadius = null;
            VFXExpression minorRadius = null;
            VFXExpression transformMatrix = null;

            foreach (var slot in allSlots)
            {
                if (slot.name.StartsWith("arcTorus")
                    || slot.name == nameof(CustomProperties.arcSequencer)
                    || slot.name == nameof(CustomProperties.heightSequencer))
                    yield return slot;

                if (slot.name == "arcTorus_torus_majorRadius")
                    majorRadius = slot.exp;
                else if (slot.name == "arcTorus_torus_minorRadius")
                    minorRadius = slot.exp;
                else if (slot.name == "arcTorus_torus_transform")
                    transformMatrix = slot.exp;
                else if (slot.name == nameof(PositionBase.ThicknessProperties.Thickness))
                    thickness = slot.exp;
            }

            yield return new VFXNamedExpression(CalculateVolumeFactor(positionBase.positionMode, majorRadius, thickness, 2.0f), "volumeFactor");

            var majorRadiusNotZero = new VFXExpressionCondition(VFXValueType.Float, VFXCondition.Greater, new VFXExpressionAbs(majorRadius), VFXOperatorUtility.EpsilonExpression[VFXValueType.Float]);
            var r = new VFXExpressionBranch(majorRadiusNotZero, VFXOperatorUtility.Saturate(minorRadius / majorRadius), VFXOperatorUtility.ZeroExpression[VFXValueType.Float]);
            yield return new VFXNamedExpression(r, "r"); // Saturate can be removed once degenerated torus are correctly handled

            var invTransposeTRS = VFXOperatorUtility.InverseTransposeTRS(transformMatrix);
            yield return new VFXNamedExpression(invTransposeTRS, "arcTorus_torus_inverseTranspose");
        }

        public override string GetSource(PositionShape positionBase)
        {

            string outSource = @"";
            if (positionBase.spawnMode == PositionShape.SpawnMode.Random)
            {
                outSource += @"float3 u = RAND3;";
                outSource += @"float arc = arcTorus_arc;";
            }
            else
            {
                outSource += @"float3 u = float3(heightSequencer, 1.0f, RAND);";
                outSource += @"float arc = arcTorus_arc * arcSequencer;";
            }

            outSource += @"
float R = sqrt(volumeFactor + (1.0f - volumeFactor) * u.z);

float sinTheta,cosTheta;
sincos(u.x * UNITY_TWO_PI, sinTheta, cosTheta);

float2 s1_1 = R * r * float2(cosTheta, sinTheta) + float2(1, 0);
float2 s1_2 = R * r * float2(-cosTheta, sinTheta) + float2(1, 0);
float w = s1_1.x / (s1_1.x + s1_2.x);

float3 t;
float phi;
if (u.y < w)
{
    phi = arc * u.y / w;
    t = float3(s1_1.x, 0, s1_1.y);
}
else
{
    phi = arc * (u.y - w) / (1.0f - w);
    t = float3(s1_2.x, 0, s1_2.y);
}

float cosPhi, sinPhi;
sincos(phi, cosPhi, sinPhi);

float3 finalDir = float3(cosPhi * t.x - sinPhi * t.y, cosPhi * t.y + sinPhi * t.x, t.z);
float3 finalPos = arcTorus_torus_majorRadius * finalDir;

finalPos = mul(arcTorus_torus_transform, float4(finalPos, 1.0f)).xyz;

float3 currentAxisY = float3(-cosPhi * cosTheta, -sinPhi * cosTheta, sinTheta);
float3 currentAxisZ = float3((t.x + t.y*cosTheta)*sinPhi, -(t.x + t.y*cosTheta)*cosPhi, 0);

finalDir = mul(arcTorus_torus_inverseTranspose, float4(finalDir, 0.0f)).xyz;
currentAxisY = mul(arcTorus_torus_inverseTranspose, float4(currentAxisY, 0.0f)).xyz;
currentAxisZ = mul(arcTorus_torus_inverseTranspose, float4(currentAxisZ, 0.0f)).xyz;
finalDir = normalize(finalDir);
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
                outSource += string.Format(positionBase.composeDirectionFormatString, "finalDir");
            }

            return outSource;
        }
    }
}
