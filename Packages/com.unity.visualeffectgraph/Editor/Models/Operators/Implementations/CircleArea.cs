using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-Area(Circle)")]
    [VFXInfo(name = "Area (Circle)", category = "Math/Geometry")]
    class CircleArea : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("Sets the circle used for the area calculation.")]
            public TCircle circle = TCircle.defaultValue;
        }

        public class OutputProperties
        {
            [Tooltip("Outputs the area of the circle.")]
            public float area;
        }

        override public string name { get { return "Area (Circle)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var scale = new VFXExpressionExtractScaleFromMatrix(inputExpression[0]);
            var radius = inputExpression[1];
            return new VFXExpression[] { VFXOperatorUtility.CircleArea(radius, scale) };
        }
    }
}
