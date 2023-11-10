using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-Volume(OrientedBox)")]
    [VFXInfo(name = "Volume (Oriented Box)", category = "Math/Geometry")]
    class OrientedBoxVolume : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("Sets the box used for the volume calculation.")]
            public OrientedBox box = new OrientedBox();
        }

        public class OutputProperties
        {
            [Tooltip("Outputs the volume of the box.")]
            public float volume;
        }

        override public string name { get { return "Volume (Oriented Box)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new VFXExpression[] { VFXOperatorUtility.BoxVolume(new VFXExpressionExtractScaleFromMatrix(inputExpression[0])) };
        }
    }
}
