using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Position")]
    class PositionAABox : PositionBase
    {
        public override string name { get { return string.Format(base.name, "Axis-Aligned Box"); ; } }

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

                if (positionMode == PositionMode.ThicknessAbsolute || positionMode == PositionMode.ThicknessRelative)
                {
                    VFXExpression factor = VFXValue.Constant(Vector3.zero);
                    VFXExpression boxSize = inputSlots[0][1].GetExpression();

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
                    outSource = "float3 rand = (RAND3 - 0.5f);\n";
                    outSource += string.Format(composeDirectionFormatString, "normalize(Box_size * rand)");
                    outSource += string.Format(composePositionFormatString, "Box_size * rand + Box_center");
                }
                else if (positionMode == PositionMode.Surface)
                {
                    outSource = @"
float areaXY = max(Box_size.x * Box_size.y, VFX_EPSILON);
float areaXZ = max(Box_size.x * Box_size.z, VFX_EPSILON);
float areaYZ = max(Box_size.y * Box_size.z, VFX_EPSILON);

float face = RAND * (areaXY + areaXZ + areaYZ);
float flip = (RAND >= 0.5f) ? 0.5f : -0.5f;
float3 cube = float3(RAND2 - 0.5f, flip);

float3 mask = float3(0,0,0);
if (face < areaXY)
{
    mask.z = 1;
    cube = cube.xyz;
}
else if(face < areaXY + areaXZ)
{
    mask.y = 1;
    cube = cube.xzy;
}
else
{
    mask.x = 1;
    cube = cube.zxy;
}
";
                    outSource += string.Format(composeDirectionFormatString, "(floor(cube+0.5f)*2-1)* mask");
                    outSource += string.Format(composePositionFormatString, "cube * Box_size + Box_center");

                }
                else
                {
                    outSource = @"
float face = RAND * cumulativeVolumes.z;
float flip = (RAND >= 0.5f) ? 1.0f : -1.0f;
float3 cube = float3(RAND2 * 2.0f - 1.0f, -RAND);

float3 mask = float3(0,0,0);
if (face < cumulativeVolumes.x)
{
    mask.z = flip;
    cube = (cube * volumeXY).xyz + float3(0.0f, 0.0f, Box_size.z);
    cube.z *= flip;
}
else if(face < cumulativeVolumes.y)
{
    mask.y = flip;
    cube = (cube * volumeXZ).xzy + float3(0.0f, Box_size.y, 0.0f);
    cube.y *= flip;
}
else
{
    mask.x = flip;
    cube = (cube * volumeYZ).zxy + float3(Box_size.x, 0.0f, 0.0f);
    cube.x *= flip;
}


";
                    outSource += string.Format(composeDirectionFormatString, "mask");
                    outSource += string.Format(composePositionFormatString, "cube * 0.5f + Box_center");
                }
                return outSource;
            }
        }


        public override void Sanitize(int version)
        {
            if(version < 5)
            {
                // SANITIZE : if older version, ensure position composition is overwrite.
                compositionPosition = AttributeCompositionMode.Overwrite;
            }
            base.Sanitize(version);
        }

    }
}
