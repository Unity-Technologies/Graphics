using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(name = "Split Matrix", category = "Math/Vector")]
    class SplitMatrix : VFXOperator
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
            [Tooltip("The matrix to split in vectors.")]
            public Matrix4x4 m = Matrix4x4.identity;
        }

        public class OutputProperties
        {
            [Tooltip("The first vector of the matrix.")]
            public Vector4 m0 = new Vector4(1, 0, 0, 0);
            [Tooltip("The second vector of the matrix.")]
            public Vector4 m1 = new Vector4(0, 1, 0, 0);
            [Tooltip("The third vector of the matrix.")]
            public Vector4 m2 = new Vector4(0, 0, 1, 0);
            [Tooltip("The fourth vector of the matrix.")]
            public Vector4 m3 = new Vector4(0, 0, 0, 1);
        }

        override public string name { get { return "Split Matrix"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var matExp = inputExpression[0];
            VFXExpression m0, m1, m2, m3;

            if (order == Order.Row)
            {
                m0 = new VFXExpressionMatrixToRow(matExp, VFXValue.Constant(0));
                m1 = new VFXExpressionMatrixToRow(matExp, VFXValue.Constant(1));
                m2 = new VFXExpressionMatrixToRow(matExp, VFXValue.Constant(2));
                m3 = new VFXExpressionMatrixToRow(matExp, VFXValue.Constant(3));
            }
            else
            {
                m0 = new VFXExpressionMatrixToColumn(matExp, VFXValue.Constant(0));
                m1 = new VFXExpressionMatrixToColumn(matExp, VFXValue.Constant(1));
                m2 = new VFXExpressionMatrixToColumn(matExp, VFXValue.Constant(2));
                m3 = new VFXExpressionMatrixToColumn(matExp, VFXValue.Constant(3));
            }

            return new[] { m0, m1, m2, m3 };
        }
    }
}

