using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Position", variantProvider = typeof(PositionBaseProvider))]
    class PositionAABox : PositionBase
    {
        public override string name { get { return string.Format(base.name, "AABox");; } }

        public class InputProperties
        {
            [Tooltip("Sets the box used for positioning the particles.")]
            public AABox Box = new AABox() { size = Vector3.one };
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                foreach (var p in GetExpressionsFromSlots(this).Where(e => e.name != "Thickness"))
                    yield return p;

                VFXExpression boxSize = inputSlots[0][1].GetExpression();

                if (positionMode == PositionMode.ThicknessAbsolute || positionMode == PositionMode.ThicknessRelative)
                {
                    VFXExpression factor = VFXValue.Constant(Vector3.zero);
                    switch (positionMode)
                    {
                        case PositionMode.ThicknessAbsolute:
                            factor = VFXOperatorUtility.Clamp(VFXOperatorUtility.CastFloat(inputSlots[1].GetExpression() * VFXValue.Constant(2.0f), VFXValueType.Float3), VFXValue.Constant(0.0f), boxSize);
                            break;
                        case PositionMode.ThicknessRelative:
                            factor = VFXOperatorUtility.CastFloat(VFXOperatorUtility.Saturate(inputSlots[1].GetExpression()), VFXValueType.Float3) * boxSize;
                            break;
                    }

                    factor = new VFXExpressionMax(factor, VFXValue.Constant(new Vector3(0.0001f, 0.0001f, 0.0001f)));

                    VFXExpression volumeXY = new VFXExpressionCombine(boxSize.x, boxSize.y, factor.z);
                    VFXExpression volumeXZ = new VFXExpressionCombine(boxSize.x, boxSize.z - factor.z, factor.y);
                    VFXExpression volumeYZ = new VFXExpressionCombine(boxSize.y - factor.y, boxSize.z - factor.z, factor.x);

                    VFXExpression volumes = new VFXExpressionCombine(
                        volumeXY.x * volumeXY.y * volumeXY.z,
                        volumeXZ.x * volumeXZ.y * volumeXZ.z,
                        volumeYZ.x * volumeYZ.y * volumeYZ.z
                    );
                    VFXExpression cumulativeVolumes = new VFXExpressionCombine(
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
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                yield return "spawnMode";
            }
        }
        protected override bool needDirectionWrite => true;

        public override string source
        {
            get
            {
                string outSource;

                if (positionMode == PositionMode.Volume)
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

float3 outDir = float3(0,0,1);
float min = top;
if (bottom < min) { outDir = float3(0, 0,-1);  min = bottom; }
if (front  < min) { outDir = float3(0, 1, 0);  min = front;  }
if (back   < min) { outDir = float3(0,-1, 0);  min = back;   }
if (right  < min) { outDir = float3(1, 0, 0);  min = right;  }
if (left   < min) { outDir = float3(-1,0, 0);  min = left;   }
";
                }
                else if (positionMode == PositionMode.Surface)
                {
                    outSource = @"
float areaXY = max(Box_size.x * Box_size.y, VFX_EPSILON);
float areaXZ = max(Box_size.x * Box_size.z, VFX_EPSILON);
float areaYZ = max(Box_size.y * Box_size.z, VFX_EPSILON);

float face = RAND * (areaXY + areaXZ + areaYZ);
float flip = (RAND >= 0.5f) ? 1.0f : -1.0f;
float3 cube = float3(RAND2 - 0.5f, flip * 0.5f);

float3 outDir;
if (face < areaXY)
{
    cube = cube.xyz;
    outDir = float3(0, 0, flip);
}
else if(face < areaXY + areaXZ)
{
    cube = cube.xzy;
    outDir = float3(0, flip, 0);
}
else
{
    cube = cube.zxy;
    outDir = float3(flip, 0, 0);
}
float3 outPos = cube * Box_size;
";
                }
                else if (positionMode == PositionMode.ThicknessAbsolute || positionMode == PositionMode.ThicknessRelative)
                {
                    outSource = @"
float face = RAND * cumulativeVolumes.z;
float flip = (RAND >= 0.5f) ? 1.0f : -1.0f;
float3 cube = float3(RAND2 * 2.0f - 1.0f, -RAND);

float3 outDir;
if (face < cumulativeVolumes.x)
{
    cube = (cube * volumeXY).xyz + float3(0.0f, 0.0f, Box_size.z);
    cube.z *= flip;
    outDir = float3(0, 0, flip);
}
else if(face < cumulativeVolumes.y)
{
    cube = (cube * volumeXZ).xzy + float3(0.0f, Box_size.y, 0.0f);
    cube.y *= flip;
    outDir = float3(0, flip, 0);
}
else
{
    cube = (cube * volumeYZ).zxy + float3(Box_size.x, 0.0f, 0.0f);
    cube.x *= flip;
    outDir = float3(flip, 0, 0);
}
float3 outPos = cube * 0.5f;
";
                }
                else
                {
                    throw new NotImplementedException();
                }

                outSource += string.Format(composeDirectionFormatString, "outDir");
                outSource += string.Format(composePositionFormatString, "outPos + Box_center");

                return outSource;
            }
        }


        public override void Sanitize(int version)
        {
            if (version < 5)
            {
                // SANITIZE : if older version, ensure position composition is overwrite.
                compositionPosition = AttributeCompositionMode.Overwrite;
            }
            base.Sanitize(version);
        }
    }
}
