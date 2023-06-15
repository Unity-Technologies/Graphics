using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-Volume(Cone)")]
    [VFXInfo(category = "Math/Geometry")]
    class ConeVolume : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("Sets the cone used for the volume calculation.")]
            public TCone cone = TCone.defaultValue;
        }

        public class OutputProperties
        {
            [Tooltip("Outputs the volume of the cone.")]
            public float volume;
        }

        override public string name { get { return "Volume (Cone)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var scale = new VFXExpressionExtractScaleFromMatrix(inputExpression[0]);
            var baseRadius = inputExpression[1];
            var topRadius = inputExpression[2];
            var height = inputExpression[3];

            return new VFXExpression[] { VFXOperatorUtility.ConeVolume(baseRadius, topRadius, height, scale) };
        }
    }
}
