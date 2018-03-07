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
            public AABox Area = AABox.defaultValue;
        }

        public override string name { get { return "Tile/Warp Positions"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kUpdateAndOutput; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }
        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.ReadWrite);
                yield return new VFXAttributeInfo(VFXAttribute.Alpha, VFXAttributeMode.ReadWrite);
            }
        }

        public override string source
        {
            get
            {
                return @"
float3 halfSize = Area_size * 0.5;

// Warp positions
float3 delta = (position - Area_center) + halfSize;
delta = delta - floor(delta / Area_size) * Area_size;
position = Area_center + delta - halfSize;
";
            }
        }
    }
}
