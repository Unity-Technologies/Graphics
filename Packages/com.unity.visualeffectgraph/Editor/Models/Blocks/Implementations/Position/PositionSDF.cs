using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    class PositionSDF : PositionShapeBase
    {
        public class InputProperties
        {
            [Tooltip("Sets the Signed Distance Field to sample from.")]
            public Texture3D SDF = VFXResources.defaultResources.signedDistanceField;
            [Tooltip("Sets the transform with which to position, scale, or rotate the field.")]
            public OrientedBox FieldTransform = OrientedBox.defaultValue;
        }

        public class CustomProperties
        {
            [Range(0, 1), Tooltip("When using customized emission, control the position around the arc to emit particles from.")]
            public float ArcSequencer = 0.0f;
        }

        public override IEnumerable<VFXNamedExpression> GetParameters(PositionShape positionBase, List<VFXNamedExpression> allSlots)
        {
            VFXExpression transform = null, sdf = null;
            foreach (var e in allSlots)
            {
                if (e.name == "FieldTransform")
                {
                    transform = e.exp;
                    yield return e;
                }

                if (e.name == "SDF")
                {
                    sdf = e.exp;
                    yield return e;
                }

                if (e.name == "ArcSequencer" ||
                    e.name == "Thickness")
                    yield return e;
            }

            var extents = new VFXNamedExpression(new VFXExpressionExtractScaleFromMatrix(transform), "extents");
            yield return new VFXNamedExpression(VFX.VFXOperatorUtility.Max3(extents.exp), "scalingFactor");
            yield return new VFXNamedExpression(new VFXExpressionInverseTRSMatrix(transform), "inverseTransform");
            var minDim = new VFXExpressionCastUintToFloat(VFX.VFXOperatorUtility.Min3(new VFXExpressionTextureHeight(sdf), new VFXExpressionTextureWidth(sdf), new VFXExpressionTextureDepth(sdf)));
            var gradStep = VFXValue.Constant(0.01f);  //kStep used in SampleSDFDerivativesFast and SampleSDFDerivatives
            var margin = VFXValue.Constant(0.5f) / minDim + gradStep + VFXValue.Constant(0.001f);
            yield return new VFXNamedExpression(VFXValue.Constant(0.5f) - margin, "projectionRayWithMargin");
            yield return new VFXNamedExpression(VFXValue.Constant(positionBase.projectionSteps), "n_steps");
        }

        public override string GetSource(PositionShape positionBase)
        {
            var outSource = new StringBuilder(@"float cosPhi = 2.0f * RAND - 1.0f;");
            if (positionBase.spawnMode == PositionBase.SpawnMode.Random)
                outSource.AppendLine(@"float theta = TWO_PI * RAND;");
            else
                outSource.AppendLine(@"float theta = TWO_PI * ArcSequencer;");
            switch (positionBase.positionMode)
            {
                case (PositionBase.PositionMode.Surface):
                    outSource.AppendLine(@" float Thickness = 0.0f;");
                    break;
                case (PositionBase.PositionMode.Volume):
                    outSource.AppendLine(@" float Thickness = scalingFactor;");
                    break;
                case (PositionBase.PositionMode.ThicknessRelative):
                    outSource.AppendLine(@" Thickness *= scalingFactor * 0.5f;");
                    break;
            }

            outSource.Append(@"
//Initialize position within texture bounds
float2 sincosTheta;
sincos(theta, sincosTheta.x, sincosTheta.y);
sincosTheta *= sqrt(1.0f - cosPhi * cosPhi);
float3 currentAxisY = float3(sincosTheta, cosPhi) *  sqrt(3)/2;
float maxDir = max(0.5f, max(abs(currentAxisY.x), max(abs(currentAxisY.y), abs(currentAxisY.z))));
currentAxisY = currentAxisY * (projectionRayWithMargin/maxDir);
float3 tPos = currentAxisY * pow(RAND, 1.0f/3.0f);
float3 coord = tPos + 0.5f;
float3 wPos, n, worldNormal;

for(uint proj_step=0; proj_step < n_steps; proj_step++){

    float dist = SampleSDF(SDF, coord);
    n = -normalize(SampleSDFDerivativesFast(SDF, coord, dist));
    dist *= scalingFactor;

    //Projection on surface/volume
    float3 delta;
    worldNormal = VFXSafeNormalize(mul(float4(n, 0), inverseTransform).xyz);
    if (dist > 0)
        delta = dist * worldNormal;
    else
    {
        delta = min(dist + Thickness, 0) * worldNormal;
    }

    wPos = mul(FieldTransform, float4(tPos,1)).xyz + delta;
    tPos = mul(inverseTransform, float4(wPos, 1)).xyz;
    coord = tPos + 0.5f;

}");
            outSource.AppendFormat(positionBase.composePositionFormatString, "wPos");

            if (positionBase.applyOrientation != PositionBase.Orientation.None)
                outSource.Append(@"currentAxisY = -worldNormal;");

            if (positionBase.applyOrientation.HasFlag(PositionBase.Orientation.Axes))
                outSource.Append(@"
float3 AxisZCandidateA = float3(-sincosTheta.y, sincosTheta.x, 0);
float3 AxisZCandidateB = float3(sign(cosPhi), 0, 0);
AxisZCandidateA = mul(float4(AxisZCandidateA, 0), inverseTransform).xyz;
AxisZCandidateB = mul(float4(AxisZCandidateB, 0), inverseTransform).xyz;

float3 currentAxisZ = AxisZCandidateA;
float3 currentAxisX = cross(currentAxisZ, currentAxisY);
if (dot(currentAxisX, currentAxisX) < 0.00001f)
{
    currentAxisZ = AxisZCandidateB;
    currentAxisX = cross(currentAxisZ, currentAxisY);
}

currentAxisX = normalize(currentAxisX);
currentAxisZ = normalize(cross(currentAxisX, currentAxisY));");

            if (positionBase.applyOrientation.HasFlag(PositionBase.Orientation.Direction))
            {
                outSource.AppendFormat(positionBase.composeDirectionFormatString, "currentAxisY");
            }

            if (positionBase.applyOrientation.HasFlag(PositionBase.Orientation.Axes))
            {
                outSource.AppendFormat(VFXBlockUtility.GetComposeString(positionBase.compositionAxes, "axisX", "currentAxisX", "blendAxes"));
                outSource.AppendFormat(VFXBlockUtility.GetComposeString(positionBase.compositionAxes, "axisY", "currentAxisY", "blendAxes"));
                outSource.AppendFormat(VFXBlockUtility.GetComposeString(positionBase.compositionAxes, "axisZ", "currentAxisZ", "blendAxes"));
            }

            if (positionBase.killOutliers)
            {
                outSource.Append(@"
 float dist = SampleSDF(SDF, coord);
 if (dist * scalingFactor > 0.01)
    alive = false;");
            }

            return outSource.ToString();
        }
    }
}
