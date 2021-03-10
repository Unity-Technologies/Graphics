using System;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    //TODO: Move this to sample mesh
    [VFXInfo(category = "Sampling")]
    class SampleDeformedMesh : VFXOperator
    {
        override public string name { get { return "Sample Deformed Mesh"; } }

        public class InputProperties
        {
            [Tooltip("Compute Mesh Index (comes from Hybrid Renderer)")]
            public uint computeMeshIndex;

            [Tooltip("ID of vertex to sample")]
            public uint vertexID;
        }

        public class OutputProperties
        {
            [Tooltip("Outputs the sampled mesh position.")]
            public Vector3 p = Vector3.zero;

            [Tooltip("Outputs the sampled mesh normal.")]
            public Vector3 n = Vector3.zero;

            [Tooltip("Outputs the sampled mesh tangent.")]
            public Vector3 t = Vector3.zero;
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new VFXExpression[]
            {
                new VFXExpressionSampleDeformedMeshPosition(inputExpression[0], inputExpression[1]),
                new VFXExpressionSampleDeformedMeshNormal(inputExpression[0], inputExpression[1]),
                new VFXExpressionSampleDeformedMeshTangent(inputExpression[0], inputExpression[1]),
            };
        }
    }
}
