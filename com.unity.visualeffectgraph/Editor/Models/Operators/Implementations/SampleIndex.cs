using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Sampling", variantProvider = typeof(SampleMeshProvider), experimental = true)]
    class SampleIndex : VFXOperator
    {
        override public string name
        {
            get
            {
                if (source == SampleMesh.SourceType.Mesh)
                    return "Sample Mesh Index";
                else
                    return "Sample Skinned Mesh Renderer Index";
            }
        }

        public class InputPropertiesMesh
        {
            [Tooltip("Sets the Mesh to sample from.")]
            public Mesh mesh = VFXResources.defaultResources.mesh;
        }

        public class InputPropertiesSkinnedMeshRenderer
        {
            [Tooltip("Sets the Mesh to sample from, has to be an exposed entry.")]
            public SkinnedMeshRenderer skinnedMesh = null;
        }

        public class InputProperties
        {
            [Tooltip("The index to read from.")]
            public uint index = 0u;
        }

        public class OutputProperties
        {
            [Tooltip("The sampled index.")]
            public uint index;
        }

        protected sealed override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var props = Enumerable.Empty<VFXPropertyWithValue>();
                if (source == SampleMesh.SourceType.Mesh)
                    props = props.Concat(PropertiesFromType(nameof(InputPropertiesMesh)));
                else if (source == SampleMesh.SourceType.SkinnedMeshRenderer)
                    props = props.Concat(PropertiesFromType(nameof(InputPropertiesSkinnedMeshRenderer)));
                else
                    throw new InvalidOperationException("Unexpected source type : " + source);
                props = props.Concat(PropertiesFromType(nameof(InputProperties)));
                return props;
            }
        }

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("Choose between source from mesh or skinned renderer mesh.")]
        private SampleMesh.SourceType source = SampleMesh.SourceType.Mesh;

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var mesh = inputExpression[0].valueType == VFXValueType.Mesh ? inputExpression[0] : new VFXExpressionMeshFromSkinnedMeshRenderer(inputExpression[0]);

            var indexFormat = new VFXExpressionMeshIndexFormat(mesh);
            var indexCount = new VFXExpressionMeshIndexCount(mesh);
            var index = VFXOperatorUtility.ApplyAddressingMode(inputExpression[1], indexCount, VFXOperatorUtility.SequentialAddressingMode.Wrap);

            var sampledIndex = new VFXExpressionSampleIndex(mesh, index, indexFormat);
            return new[] { sampledIndex };
        }
    }
}
