using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Block
{
    class PositionBox : PositionShapeBase
    {
        public sealed override bool supportCustomSpawn => false;

        public class InputProperties
        {
            [Tooltip("Sets the box used for positioning the particles.")]
            public OrientedBox Box = OrientedBox.defaultValue;
        }

        protected static IEnumerable<VFXNamedExpression> GetVolumeExpressions(PositionShape.PositionMode positionMode, VFXExpression boxSize, VFXExpression thickness)
        {
            if (positionMode == PositionShape.PositionMode.ThicknessAbsolute ||
                positionMode == PositionShape.PositionMode.ThicknessRelative)
            {
                VFXExpression factor = VFXValue.Constant(Vector3.zero);
                switch (positionMode)
                {
                    case PositionShape.PositionMode.ThicknessAbsolute:
                        factor = VFXOperatorUtility.Clamp(
                            VFXOperatorUtility.CastFloat(thickness * VFXValue.Constant(2.0f), VFXValueType.Float3),
                            VFXValue.Constant(0.0f), boxSize);
                        break;
                    case PositionShape.PositionMode.ThicknessRelative:
                        factor = VFXOperatorUtility.CastFloat(VFXOperatorUtility.Saturate(thickness),
                            VFXValueType.Float3) * boxSize;
                        break;
                    default:
                        throw new NotImplementedException();
                }

                factor = new VFXExpressionMax(factor, VFXValue.Constant(new Vector3(0.0001f, 0.0001f, 0.0001f)));

                var volumeXY = new VFXExpressionCombine(boxSize.x, boxSize.y, factor.z);
                var volumeXZ = new VFXExpressionCombine(boxSize.x, boxSize.z - factor.z, factor.y);
                var volumeYZ = new VFXExpressionCombine(boxSize.y - factor.y, boxSize.z - factor.z, factor.x);

                var volumes = new VFXExpressionCombine(
                    volumeXY.x * volumeXY.y * volumeXY.z,
                    volumeXZ.x * volumeXZ.y * volumeXZ.z,
                    volumeYZ.x * volumeYZ.y * volumeYZ.z
                );
                var cumulativeVolumes = new VFXExpressionCombine(
                    volumes.x,
                    volumes.x + volumes.y,
                    volumes.x + volumes.y + volumes.z
                );

                yield return new VFXNamedExpression(volumeXY, "volumeXY");
                yield return new VFXNamedExpression(volumeXZ, "volumeXZ");
                yield return new VFXNamedExpression(volumeYZ, "volumeYZ");
                yield return new VFXNamedExpression(cumulativeVolumes, "cumulativeVolumes");
            }
        }

        public override IEnumerable<VFXNamedExpression> GetParameters(PositionShape positionBase, List<VFXNamedExpression> allSlots)
        {
            VFXExpression transform = null;
            VFXExpression thickness = null;
            foreach (var slot in allSlots)
            {
                if (slot.name == "Box")
                    transform = slot.exp;
                else if (slot.name == nameof(PositionShape.ThicknessProperties.Thickness))
                    thickness = slot.exp;
            }

            VFXExpression boxSize;
            if (positionBase.positionMode == PositionBase.PositionMode.ThicknessAbsolute)
            {
                boxSize = new VFXExpressionExtractScaleFromMatrix(transform);
                var invBoxSize = VFXOperatorUtility.OneExpression[VFXValueType.Float3] / boxSize;
                var zero = VFXOperatorUtility.ZeroExpression[VFXValueType.Float3];
                var invScaleMatrix = new VFXExpressionTRSToMatrix(zero, zero, invBoxSize);
                transform = new VFXExpressionTransformMatrix(transform, invScaleMatrix);
            }
            else
            {
                boxSize = VFXOperatorUtility.OneExpression[VFXValueType.Float3];
            }

            //If possible, remove scale to allow zero scale matrices (see UUM-62355)
            var transformForInversion = transform;
            var isInverseTransposeBoxOrthonormal = VFXOperatorUtility.FalseExpression;
            if (positionBase.inputSlots[0].space == VFXSpace.None || positionBase.inputSlots[0].space == positionBase.GetParent().space)
            {
                if (transform is VFXExpressionTRSToMatrix)
                {
                    transformForInversion = new VFXExpressionTRSToMatrix(VFXOperatorUtility.ZeroExpression[VFXValueType.Float3], transform.parents[1], VFXOperatorUtility.OneExpression[VFXValueType.Float3]);
                    isInverseTransposeBoxOrthonormal = VFXOperatorUtility.TrueExpression;
                }
            }
            else
            {
                if (transform is not VFXExpressionTransformMatrix)
                    throw new InvalidOperationException("Unexpected missing space conversion");

                var left = transform.parents[0];
                var right = transform.parents[1];
                if (right is VFXExpressionTRSToMatrix)
                {
                    right = new VFXExpressionTRSToMatrix(VFXOperatorUtility.ZeroExpression[VFXValueType.Float3], right.parents[1], VFXOperatorUtility.OneExpression[VFXValueType.Float3]);
                    transformForInversion = new VFXExpressionTransformMatrix(left, right);
                    //inverseTransposeBoxOrthonormal still false, LocalToWorld can contains scale
                }
            }

            yield return new VFXNamedExpression(transform, "Box");
            yield return new VFXNamedExpression(VFXOperatorUtility.InverseTransposeTRS(transformForInversion), "InverseTransposeBox");
            yield return new VFXNamedExpression(isInverseTransposeBoxOrthonormal, "IsInverseTransposeBoxOrthonormal");
            yield return new VFXNamedExpression(boxSize, "Box_size");
            foreach (var p in GetVolumeExpressions(positionBase.positionMode, boxSize, thickness))
                yield return p;
        }

        public override string GetSource(PositionShape positionBase)
        {
            string outSource;
            if (positionBase.positionMode == PositionShape.PositionMode.Volume)
            {
                outSource = @"
float3 localRand3 = RAND3 - (float3)0.5f;
float3 outPos =  Box_size * localRand3;
";
                outSource += @"
float3 outPosSizeGreaterThanZero = max(Box_size, VFX_EPSILON) * localRand3;
float3 planeBound = 0.5f * Box_size;
float top    = planeBound.z - outPosSizeGreaterThanZero.z;
float bottom = planeBound.z + outPosSizeGreaterThanZero.z;
float front  = planeBound.y - outPosSizeGreaterThanZero.y;
float back   = planeBound.y + outPosSizeGreaterThanZero.y;
float right  = planeBound.x - outPosSizeGreaterThanZero.x;
float left   = planeBound.x + outPosSizeGreaterThanZero.x;

float3 outDir = float3( 0,  0, 1);
float3 outUp  = float3(-1,  0, 0);
float3 outTan = float3( 0, -1, 0);

float min = top;
if (bottom < min) { outDir = float3( 0,  0, -1); outUp = float3( -1, 0,  0); outTan = float3( 0,  1,  0); min = bottom; }
if (front  < min) { outDir = float3( 0,  1,  0); outUp = float3( -1, 0,  0); outTan = float3( 0,  0,  1); min = front;  }
if (back   < min) { outDir = float3( 0, -1,  0); outUp = float3( -1, 0,  0); outTan = float3( 0,  0, -1); min = back;   }
if (right  < min) { outDir = float3( 1,  0,  0); outUp = float3( 0,  0,  1); outTan = float3( 0, -1,  0); min = right;  }
if (left   < min) { outDir = float3(-1,  0,  0); outUp = float3( 0,  0, -1); outTan = float3( 0, -1,  0); min = left;   }
";
            }
            else if (positionBase.positionMode == PositionShape.PositionMode.Surface)
            {
                outSource = @"
float areaXY = max(Box_size.x * Box_size.y, VFX_EPSILON);
float areaXZ = max(Box_size.x * Box_size.z, VFX_EPSILON);
float areaYZ = max(Box_size.y * Box_size.z, VFX_EPSILON);

float face = RAND * (areaXY + areaXZ + areaYZ);
float flip = (RAND >= 0.5f) ? 1.0f : -1.0f;
float3 cube = float3(RAND2 - 0.5f, flip * 0.5f);

float3 outDir;
float3 outUp;
float3 outTan;

if (face < areaXY)
{
    cube = cube.xyz;
    outDir = float3(0, 0, flip);
    outUp  = float3(-1, 0, 0);
    outTan = float3(0, -flip, 0);
}
else if(face < areaXY + areaXZ)
{
    cube = cube.xzy;
    outDir = float3(0, flip, 0);
    outUp = float3(-1, 0, 0);
    outTan = float3(0, 0, flip); 
}
else
{
    cube = cube.zxy;
    outDir = float3(flip, 0, 0);
    outUp  = float3(0, 0, flip);
    outTan = float3(0, -1, 0);
}
float3 outPos = cube * Box_size;
";
            }
            else if (positionBase.positionMode == PositionShape.PositionMode.ThicknessAbsolute || positionBase.positionMode == PositionShape.PositionMode.ThicknessRelative)
            {
                outSource = @"
float face = RAND * cumulativeVolumes.z;
float flip = (RAND >= 0.5f) ? 1.0f : -1.0f;
float3 cube = float3(RAND2 * 2.0f - 1.0f, -RAND);

float3 outDir;
float3 outUp;
float3 outTan;

if (face < cumulativeVolumes.x)
{
    cube = (cube * volumeXY).xyz + float3(0.0f, 0.0f, Box_size.z);
    cube.z *= flip;
    outDir = float3(0, 0, flip);
    outUp  = float3(-1, 0, 0);
    outTan = float3(0, -flip, 0);
}
else if(face < cumulativeVolumes.y)
{
    cube = (cube * volumeXZ).xzy + float3(0.0f, Box_size.y, 0.0f);
    cube.y *= flip;
    outDir = float3(0, flip, 0);
    outUp = float3(-1, 0, 0);
    outTan = float3(0, 0, flip); 
}
else
{
    cube = (cube * volumeYZ).zxy + float3(Box_size.x, 0.0f, 0.0f);
    cube.x *= flip;
    outDir = float3(flip, 0, 0);
    outUp  = float3(0, 0, flip);
    outTan = float3(0, -1, 0);
}
float3 outPos = cube * 0.5f;
";
            }
            else
            {
                throw new NotImplementedException();
            }

            outSource += @"
outPos = mul(Box, float4(outPos, 1.0f)).xyz;
outUp = mul(InverseTransposeBox, float4(outUp, 0.0f)).xyz;
outDir = mul(InverseTransposeBox, float4(outDir, 0.0f)).xyz;
if (!IsInverseTransposeBoxOrthonormal)
{
    outDir = normalize(outDir);
    outUp = normalize(outUp);
}
outTan = cross(outDir, outUp);
";

            if (positionBase.applyOrientation.HasFlag(PositionBase.Orientation.Direction))
            {
                outSource += string.Format(positionBase.composeDirectionFormatString, "outDir");
            }

            if (positionBase.applyOrientation.HasFlag(PositionBase.Orientation.Axes))
            {
                outSource += VFXBlockUtility.GetComposeString(positionBase.compositionAxes, "axisX", "outTan", "blendAxes") + "\n";
                outSource += VFXBlockUtility.GetComposeString(positionBase.compositionAxes, "axisY", "outDir", "blendAxes") + "\n";
                outSource += VFXBlockUtility.GetComposeString(positionBase.compositionAxes, "axisZ", "outUp", "blendAxes") + "\n";
            }

            outSource += string.Format(positionBase.composePositionFormatString, "outPos");
            return outSource;
        }
    }
}
