using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Geometry")]
    class VFXOperatorDistanceToLine : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The line used for the distance calculation.")]
            public Line line = new Line();
            [Tooltip("The position used for the distance calculation.")]
            public Position position = new Position();
        }

        override public string name { get { return "Distance (Line)"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression lineDelta = new VFXExpressionSubtract(inputExpression[1], inputExpression[0]);
            VFXExpression lineLength = new VFXExpressionMax(VFXOperatorUtility.Dot(lineDelta, lineDelta), VFXValue.Constant(Mathf.Epsilon));
            VFXExpression pointProjected = new VFXExpressionMul(new VFXExpressionSubtract(inputExpression[2], inputExpression[0]), lineDelta);

            VFXExpression[] pointProjectedComponents = VFXOperatorUtility.ExtractComponents(pointProjected).ToArray();
            VFXExpression t = new VFXExpressionAdd(pointProjectedComponents[0], new VFXExpressionAdd(pointProjectedComponents[1], pointProjectedComponents[2]));
            t = VFXOperatorUtility.Clamp(new VFXExpressionDivide(t, lineLength), VFXValue.Constant(0.0f), VFXValue.Constant(1.0f));

            VFXExpression pointOnLine = new VFXExpressionAdd(inputExpression[0], new VFXExpressionMul(VFXOperatorUtility.CastFloat(t, lineDelta.ValueType), lineDelta));
            VFXExpression lineDistance = VFXOperatorUtility.Distance(inputExpression[2], pointOnLine);
            return new VFXExpression[] { pointOnLine, lineDistance };
        }
    }
}
