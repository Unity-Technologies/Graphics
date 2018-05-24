using System;

namespace UnityEditor.ShaderGraph
{
    public struct MatrixNames
    {
        public const string Model = "UNITY_MATRIX_M";
        public const string ModelInverse = "UNITY_MATRIX_I_M";
        public const string View = "UNITY_MATRIX_V";
        public const string ViewInverse = "UNITY_MATRIX_I_V";
        public const string Projection = "UNITY_MATRIX_P";
        public const string ViewProjection = "UNITY_MATRIX_VP";
        public const string ModelView = "UNITY_MATRIX_MV";
        public const string ModelViewTransposed = "UNITY_MATRIX_T_MV";
        public const string ModelViewInverseTransposed = "UNITY_MATRIX_IT_MV";
        public const string ModelViewProjection = "UNITY_MATRIX_MVP";
    }
}
