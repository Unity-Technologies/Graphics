using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-Volume(Torus)")]
    [VFXInfo(name = "Volume (Torus)", category = "Math/Geometry")]
    class TorusVolume : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("Sets the torus used for the volume calculation.")]
            public TTorus torus = TTorus.defaultValue;
        }

        public class OutputProperties
        {
            [Tooltip("Outputs the volume of the torus.")]
            public float volume;
        }

        override public string name { get { return "Volume (Torus)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var scale = new VFXExpressionExtractScaleFromMatrix(inputExpression[0]);
            var majorRadius = inputExpression[1];
            var minorRadius = inputExpression[2];
            return new VFXExpression[] { VFXOperatorUtility.TorusVolume(majorRadius, minorRadius, scale) };
        }
    }
}
