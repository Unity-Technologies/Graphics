using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Position")]
    class PositionLine : PositionBase
    {
        public override string name { get { return "Position (Line)"; } }

        public class InputProperties
        {
            [Tooltip("Sets the line used for positioning the particles.")]
            public Line line = new Line() { start = Vector3.zero, end = Vector3.right };
        }

        public class CustomProperties
        {
            [Range(0, 1), Tooltip("Sets the position along the line to emit particles from when ‘Custom Emission’ is used.")]
            public float LineSequencer = 0.0f;
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                yield return "positionMode";
            }
        }

        public override string source
        {
            get
            {
                if (spawnMode == SpawnMode.Custom)
                    return @"position += lerp(line_start, line_end, LineSequencer);";
                else
                    return @"position += lerp(line_start, line_end, RAND);";
            }
        }
    }
}
