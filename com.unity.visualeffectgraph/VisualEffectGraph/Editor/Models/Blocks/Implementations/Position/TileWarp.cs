using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.VFX;
using System;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Position")]
    class TileWarp : VFXBlock
    {
        public class InputProperties
        {
            [Tooltip("Volume that will contain the tiled/warped particles")]
            public AABox Volume = AABox.defaultValue;
        }

        public override string name { get { return "Tile/Warp Positions"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kUpdateAndOutput; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }
        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.ReadWrite);
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
delta = delta - floor(delta / Volume_size) * Volume_size;
position = Volume_center + delta - halfSize;
";
            }
        }
    }
}
