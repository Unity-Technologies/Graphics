using System;
using UnityEngine;

namespace UnityEditor.VFX.Operator
{
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
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { new VFXExpressionSampleDeformedMeshData(inputExpression[0], inputExpression[1]) };
        }
    }
}
