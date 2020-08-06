using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Sampling", experimental = true)]
    class MeshVertexCount : VFXOperator
    {
        override public string name { get { return "Mesh Vertex Count"; } }

        public class InputProperties
        {
            [Tooltip("Sets the Mesh to sample from.")]
            public Mesh mesh = VFXResources.defaultResources.mesh;
        }

        public class OutputProperties
        {
            [Tooltip("Outputs the number of vertices in the Mesh.")]
            public uint count;
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var meshVertexCount = new VFXExpressionMeshVertexCount(inputExpression[0]);
            return new VFXExpression[] { meshVertexCount };
        }
    }
}
