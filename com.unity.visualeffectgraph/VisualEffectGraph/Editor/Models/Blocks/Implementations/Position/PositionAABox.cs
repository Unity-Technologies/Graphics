using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Position")]
    class PositionAABox : PositionBase
    {
        public override string name { get { return "Position (AABox)"; } }

        public class InputProperties
        {
            [Tooltip("The box used for positioning particles.")]
            public AABox Box = new AABox() { size = Vector3.one };
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                foreach (var p in GetExpressionsFromSlots(this).Where(e => e.name != "Thickness"))
                    yield return p;

                VFXExpression factor = VFXValue.Constant(Vector3.zero);

                switch (positionMode)
                {
                    case PositionMode.Surface:
                        factor = VFXValue.Constant(Vector3.zero);
                        break;
                    case PositionMode.Volume:
                        factor = VFXValue.Constant(Vector3.one);
                        break;
                    case PositionMode.ThicknessAbsolute:
                    case PositionMode.ThicknessRelative:
                    {
                        var thickness = VFXOperatorUtility.CastFloat(inputSlots[1].GetExpression(), VFXValueType.kFloat3);
                        if (positionMode == PositionMode.ThicknessAbsolute)
                        {
                            var size = inputSlots[0][1].GetExpression();
                            thickness = VFXOperatorUtility.CastFloat(thickness, VFXValueType.kFloat3) / size;
                        }

                        factor = VFXOperatorUtility.Saturate(thickness);
                        break;
                    }
                }

                yield return new VFXNamedExpression(new VFXExpressionPow(VFXValue.Constant(Vector3.one) - factor, VFXValue.Constant(new Vector3(3, 3, 3))), "volumeFactor");
            }
        }

        public override string source
        {
            get
            {
                if (positionMode == PositionMode.Volume)
                {
                    return @"position = Box_size * (RAND3 - 0.5f) + Box_center;";
                }
                else
                {
                    string outSource = @"
float areaXY = Box_size.x * Box_size.y;
float areaXZ = Box_size.x * Box_size.z;
float areaYZ = Box_size.y * Box_size.z;

float face = RAND * (areaXY + areaXZ + areaYZ);
float flip = (RAND >= 0.5f) ? 0.5f : -0.5f;
float3 cube = float3(RAND2 - 0.5f, flip);

if (face < areaXY)
    cube = cube.xyz;
else if(face < areaXY + areaXZ)
    cube = cube.xzy;
else
    cube = cube.zyx;
";

                    if (positionMode == PositionMode.Surface)
                    {
                        outSource += @"position = cube * Box_size + Box_center;";
                    }
                    else
                    {
                        outSource += @"
float3 vNorm = pow(volumeFactor + (1 - volumeFactor) * RAND, 1.0f/3.0f);
position = cube * Box_size * vNorm + Box_center;
";
                    }

                    return outSource;
                }
            }
        }
    }
}
