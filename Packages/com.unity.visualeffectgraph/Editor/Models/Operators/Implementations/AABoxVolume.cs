using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-Volume(AxisAlignedBox)")]
    [VFXInfo(category = "Math/Geometry")]
    class AABoxVolume : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("Sets the box used for the volume calculation.")]
            public AABox box = new AABox();
        }

        public class OutputProperties
        {
            [Tooltip("Outputs the volume of the box.")]
            public float volume;
        }

        override public string name { get { return "Volume (Axis Aligned Box)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new VFXExpression[] { VFXOperatorUtility.BoxVolume(inputExpression[1]) };
        }
    }
}
