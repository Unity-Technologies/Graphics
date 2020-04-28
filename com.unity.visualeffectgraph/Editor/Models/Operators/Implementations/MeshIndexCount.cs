using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Sampling", experimental = true)]
    class MeshIndexCount : VFXOperator
    {
        override public string name { get { return "Mesh Index Count"; } }

        public class InputProperties
        {
            [Tooltip("Sets the mesh to sample from.")]
            public Mesh mesh = VFXResources.defaultResources.mesh;
        }

        public class OutputProperties
        {
            [Tooltip("The number of indices in this mesh")]
            public uint count;
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var meshIndexCount = new VFXExpressionMeshIndexCount(inputExpression[0]);
            return new VFXExpression[] { meshIndexCount };
        }
    }
}
