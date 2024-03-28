using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-ConstructMatrix")]
    [VFXInfo(name = "Construct Matrix", category = "Math/Vector")]
    class ConstructMatrix : VFXOperator
    {
        public enum Order
        {
            Row,
            Column,
        }

        [VFXSetting, Tooltip("Specifies the order of the vectors, either row or column")]
        public Order order = Order.Column;

        public class InputProperties
        {
            [Tooltip("The first vector of the matrix to construct.")]
            public Vector4 m0 = new Vector4(1, 0, 0, 0);
            [Tooltip("The second vector of the matrix to construct.")]
            public Vector4 m1 = new Vector4(0, 1, 0, 0);
            [Tooltip("The third vector of the matrix to construct.")]
            public Vector4 m2 = new Vector4(0, 0, 1, 0);
            [Tooltip("The fourth vector of the matrix to construct.")]
            public Vector4 m3 = new Vector4(0, 0, 0, 1);
        }
        public class OutputProperties
        {
            public Matrix4x4 m = Matrix4x4.identity;
        }

        override public string name { get { return "Construct Matrix"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression m0 = inputExpression[0];
            VFXExpression m1 = inputExpression[1];
            VFXExpression m2 = inputExpression[2];
            VFXExpression m3 = inputExpression[3];

            if (order == Order.Row)
                return new[] { new VFXExpressionRowToMatrix(m0, m1, m2, m3) };
            else
                return new[] { new VFXExpressionColumnToMatrix(m0, m1, m2, m3) };
        }
    }
}
