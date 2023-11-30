using System.Collections.Generic;

using UnityEngine;

namespace UnityEditor.VFX.Block
{
    class PositionLineDeprecatedV2 : PositionBase
    {
        public override void Sanitize(int version)
        {
            var newPositionShape = ScriptableObject.CreateInstance<PositionShape>();
            SanitizeHelper.MigrateBlockPositionToComposed(GetGraph(), GetParent().position, newPositionShape, this, PositionShapeBase.Type.Line);
            ReplaceModel(newPositionShape, this);
        }

        public override string name { get { return string.Format(base.name, "Line"); } }

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

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                VFXExpression line_start = null;
                VFXExpression line_end = null;
                foreach (var param in base.parameters)
                {
                    if (param.name == "line_start")
                        line_start = param.exp;
                    if (param.name == "line_end")
                        line_end = param.exp;

                    yield return param;
                }
                var line_direction = VFXOperatorUtility.SafeNormalize(line_end - line_start);
                yield return new VFXNamedExpression(line_direction, "line_direction");
            }
        }

        protected override bool needDirectionWrite => true;

        public override string source
        {
            get
            {
                string outSource;
                if (spawnMode == SpawnMode.Custom)
                    outSource = string.Format(composePositionFormatString, "lerp(line_start, line_end, LineSequencer)");
                else
                    outSource = string.Format(composePositionFormatString, "lerp(line_start, line_end, RAND)");
                outSource += "\n";
                outSource += string.Format(composeDirectionFormatString, "line_direction");
                return outSource;
            }
        }
    }
}
