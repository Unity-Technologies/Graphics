using System;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-LookAT")]
    [VFXInfo(category = "Math/Vector", synonyms = new []{ "Orient" })]
    class LookAt : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The eye position.")]
            public Position from = new Position() { position = Vector3.zero };
            [Tooltip("The target position.")]
            public Position to = new Position() { position = Vector3.one };
            [Normalize, Tooltip("The up vector.")]
            public DirectionType up = Vector3.up;
        }

        public class OutputProperties
        {
            public Transform o = Transform.defaultValue;
        }

        public override string name => "Look At";

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression from = inputExpression[0];
            VFXExpression to = inputExpression[1];
            VFXExpression up = inputExpression[2];

            VFXExpression viewVector = to - from;

            VFXExpression z = VFXOperatorUtility.SafeNormalize(viewVector);
            VFXExpression x = VFXOperatorUtility.SafeNormalize(VFXOperatorUtility.Cross(up, z));
            VFXExpression y = VFXOperatorUtility.Cross(z, x);

            VFXExpression matrix = new VFXExpressionAxisToMatrix(x, y, z, from);
            return new[] { matrix };
        }

        internal sealed override void GenerateErrors(VFXErrorReporter report)
        {
            var context = new VFXExpression.Context(VFXExpressionContextOption.ConstantFolding);
            var fromExpr = GetInputSlot(0).GetExpression();
            var toExpr = GetInputSlot(1).GetExpression();
            var upExpr = GetInputSlot(2).GetExpression();
            context.RegisterExpression(fromExpr);
            context.RegisterExpression(toExpr);
            context.RegisterExpression(upExpr);
            context.Compile();

            if (context.GetReduced(fromExpr) is { } from && from.Is(VFXExpression.Flags.Constant) &&
                context.GetReduced(toExpr) is { } to && to.Is(VFXExpression.Flags.Constant))
            {

                if ((from.Get<Vector3>() - to.Get<Vector3>()).sqrMagnitude < Mathf.Epsilon)
                {
                    report.RegisterError("LookAtFromEqualTo", VFXErrorType.Error, "From and To positions cannot be equal", this);
                }
            }

            if (context.GetReduced(upExpr) is { } up && up.Is(VFXExpression.Flags.Constant))
            {
                var sqrLength = up.Get<Vector3>().sqrMagnitude;
                if (sqrLength is 0 or float.NaN)
                {
                    report.RegisterError("LookAtUpIsZeroLength", VFXErrorType.Error, "Up vector cannot be zero length", this);
                }
            }
        }
    }
}
