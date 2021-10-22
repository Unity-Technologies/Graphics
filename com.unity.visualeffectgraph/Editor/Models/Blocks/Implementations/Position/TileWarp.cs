using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using UnityEditor.VFX;
using System;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Attribute/position/TileWarp")]
    class TileWarp : VFXBlock
    {
        public class InputProperties
        {
            [Tooltip("Sets the volume which will contain the tiled/warped particles. Particles which exit the volume from one side will reappear on the opposite side.")]
            public AABox Volume = AABox.defaultValue;
        }

        public override string name { get { return "Tile/Warp Positions"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.UpdateAndOutput; } }
        public override VFXDataType compatibleData { get { return VFXDataType.Particle; } }
        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.ReadWrite);
            }
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                foreach (var param in GetExpressionsFromSlots(this))
                    yield return param;

                yield return new VFXNamedExpression(VFXOperatorUtility.OneExpression[VFXValueType.Float3] / inputSlots[0][1].GetExpression(), "invVolumeSize");
            }
        }

        public override string source
        {
            get
            {
                return @"
float3 halfSize = Volume_size * 0.5;

// Warp positions
float3 delta = (position - Volume_center) + halfSize;
delta = frac(delta * invVolumeSize) * Volume_size;
position = Volume_center + delta - halfSize;
";
            }
        }
    }
}
