using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Position")]
    class PositionLine : VFXBlock
    {
        public override string name { get { return "Position (Line)"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kInitAndUpdateAndOutput; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }
        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.ReadWrite);
                yield return new VFXAttributeInfo(VFXAttribute.Seed, VFXAttributeMode.ReadWrite);
            }
        }

        public class InputProperties
        {
            [Tooltip("The line used for positioning particles.")]
            public Line line = new Line() { start = Vector3.zero, end = Vector3.right };
        }

        public override string source
        {
            get
            {
                return @"position += lerp(line_start, line_end, RAND);";
            }
        }
    }
}
