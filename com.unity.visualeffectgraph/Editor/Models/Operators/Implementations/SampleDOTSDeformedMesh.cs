using UnityEditor.VFX;
using UnityEngine;
using UnityEngine.Timeline;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Sampling", experimental = true)]
    class SampleDOTSDeformedMesh : VFXOperator
    {
        override public string name
        {
            get { return "Sample DOTS Deformed Mesh"; }
        }

        public class InputProperties
        {
            [Tooltip("Sets the Skinned mesh renderer to sample from.")]
            public Mesh mesh;

            [Tooltip("Sets the vertex index to read from.")]
            public uint vertex = 0u;

            [Tooltip("Deformed Mesh Index (comes from Hybrid Renderer)")]
            public Vector4 deformedMeshIndex = Vector4.zero;
        }

        public class OutputProperties
        {
            [Tooltip("Deformed mesh position")] public Vector3 position = Vector3.zero;
            [Tooltip("Deformed mesh normal")] public Vector3 normal = Vector3.zero;
            [Tooltip("Deformed mesh tangent")] public Vector3 tangent = Vector3.zero;
        }

        [VFXSetting, SerializeField,
         Tooltip(
             "Specifies how Unity handles the sample when the custom vertex index is out the out of bounds of the vertex array.")]
        private VFXOperatorUtility.SequentialAddressingMode
            mode = VFXOperatorUtility.SequentialAddressingMode.Clamp;

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var mesh = inputExpression[0];

            var meshVertexCount = new VFXExpressionMeshVertexCount(mesh);
            var vertexIndex = VFXOperatorUtility.ApplyAddressingMode(inputExpression[1], meshVertexCount, mode);

            return new VFXExpression[]
            {
                new VFXExpressionSampleDeformedMeshPosition(inputExpression[2], vertexIndex),
                new VFXExpressionSampleDeformedMeshNormal(inputExpression[2], vertexIndex),
                new VFXExpressionSampleDeformedMeshTangent(inputExpression[2], vertexIndex),
            };
        }
    }
}
