using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-ConstructMatrix")]
    [VFXInfo(category = "Math/Vector")]
    class ConstructMatrix : VFXOperator
    {
        public class InputProperties
        {
            [Tooltip("The first column of the matrix to construct.")]
            public Vector4 c0 = new Vector4(1, 0, 0, 0);
            [Tooltip("The second column of the matrix to construct.")]
            public Vector4 c1 = new Vector4(0, 1, 0, 0);
            [Tooltip("The third column of the matrix to construct.")]
            public Vector4 c2 = new Vector4(0, 0, 1, 0);
            [Tooltip("The fourth column of the matrix to construct.")]
            public Vector4 c3 = new Vector4(0, 0, 0, 1);
        }
        public class OutputProperties
        {
            public Matrix4x4 m = Matrix4x4.identity;
        }
        override public string name { get { return "Construct Matrix"; } }

        override protected VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            VFXExpression c0 = inputExpression[0];
            VFXExpression c1 = inputExpression[1];
            VFXExpression c2 = inputExpression[2];
            VFXExpression c3 = inputExpression[3];

            VFXExpression matrix = new VFXExpressionVector4sToMatrix(c0, c1, c2, c3);
            return new[] { matrix };
        }
    }
}
