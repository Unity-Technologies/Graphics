using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Block
{
    sealed class PositionLine : PositionShapeBase
    {
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

        public override bool supportVolume => false;

        public override IEnumerable<VFXNamedExpression> GetParameters(PositionShape positionBase, List<VFXNamedExpression> allSlots)
        {
            VFXExpression line_start = null;
            VFXExpression line_end = null;
            foreach (var param in allSlots)
            {
                if (param.name.StartsWith("line")
                    || param.name == nameof(CustomProperties.LineSequencer))
                    yield return param;

                if (param.name == "line_start")
                    line_start = param.exp;
                if (param.name == "line_end")
                    line_end = param.exp;
            }

            var preferredUp = VFXValue.Constant(Vector3.up);
            var fallbackUp = VFXValue.Constant(Vector3.right);

            var line_direction = VFXOperatorUtility.SafeNormalize(line_end - line_start);

            var line_tangent = VFXOperatorUtility.Cross(preferredUp, line_direction);
            var line_tangent_srq_length = VFXOperatorUtility.Dot(line_tangent, line_tangent);
            var line_tangent_srq_length_close_to_zero = new VFXExpressionCondition(VFXValueType.Float, VFXCondition.Less, line_tangent_srq_length, VFXOperatorUtility.EpsilonSqrExpression[VFXValueType.Float]);
            line_tangent = new VFXExpressionBranch(line_tangent_srq_length_close_to_zero, VFXOperatorUtility.Cross(fallbackUp, line_direction), line_tangent);
            line_tangent = VFXOperatorUtility.Normalize(line_tangent);
            var line_up = VFXOperatorUtility.Cross(line_tangent, line_direction);

            if (positionBase.applyOrientation.HasFlag(PositionBase.Orientation.Direction))
            {
                yield return new VFXNamedExpression(line_direction, "line_direction");
            }

            if (positionBase.applyOrientation.HasFlag(PositionBase.Orientation.Axes))
            {
                yield return new VFXNamedExpression(line_tangent, "line_tangent");
                if (!positionBase.applyOrientation.HasFlag(PositionBase.Orientation.Direction))
                    yield return new VFXNamedExpression(line_direction, "line_direction");
                yield return new VFXNamedExpression(line_up, "line_up");
            }

        }

        public override string GetSource(PositionShape positionBase)
        {
            string outSource;
            if (positionBase.spawnMode == PositionShape.SpawnMode.Custom)
                outSource = string.Format(positionBase.composePositionFormatString, "lerp(line_start, line_end, LineSequencer)");
            else
                outSource = string.Format(positionBase.composePositionFormatString, "lerp(line_start, line_end, RAND)");

            if (positionBase.applyOrientation.HasFlag(PositionBase.Orientation.Direction))
            {
                outSource += "\n";
                outSource += string.Format(positionBase.composeDirectionFormatString, "line_direction");
            }

            if (positionBase.applyOrientation.HasFlag(PositionBase.Orientation.Axes))
            {
                outSource += VFXBlockUtility.GetComposeString(positionBase.compositionAxes, "axisX", "line_tangent", "blendAxes") + "\n";
                outSource += VFXBlockUtility.GetComposeString(positionBase.compositionAxes, "axisY", "line_direction", "blendAxes") + "\n";
                outSource += VFXBlockUtility.GetComposeString(positionBase.compositionAxes, "axisZ", "line_up", "blendAxes") + "\n";
            }

            return outSource;
        }
    }
}
