using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.VFX.Operator
{
#if UNITY_2020_2_OR_NEWER
    [VFXInfo(category = "Sampling", experimental = true)]
    class SampleIndex : VFXOperator
    {
        override public string name { get { return "Sample Index"; } }

        public class InputProperties
        {
            [Tooltip("Sets the mesh to sample from.")]
            public Mesh mesh = VFXResources.defaultResources.mesh;
            [Tooltip("The index to read from.")]
            public uint index = 0u;
        }

        public class OutputProperties
        {
            [Tooltip("The sampled index.")]
            public uint index;
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var mesh = inputExpression[0];

            var indexFormat = new VFXExpressionMeshIndexFormat(mesh);
            var indexCount = new VFXExpressionMeshIndexCount(mesh);
            var index = VFXOperatorUtility.ApplyAddressingMode(inputExpression[1], indexCount, VFXOperatorUtility.SequentialAddressingMode.Wrap);

            var sampledIndex = new VFXExpressionSampleIndex(mesh, index, indexFormat);
            return new[] { sampledIndex };
        }
    }
#endif
}
