using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Sampling", variantProvider = typeof(SampleMeshProvider), experimental = true)]
    class MeshVertexCount : VFXOperator
    {
        override public string name
        {
            get
            {
                if (source == SampleMesh.SourceType.Mesh)
                    return "Mesh Vertex Count";
                else
                    return "Skinned Mesh Vertex Count";
            }
        }

        public class InputPropertiesMesh
        {
            [Tooltip("Specifies the Mesh to sample from.")]
            public Mesh mesh = VFXResources.defaultResources.mesh;
        }

        public class InputPropertiesSkinnedMeshRenderer
        {
            [Tooltip("Specifies the Skinned Mesh Renderer component to sample from. The Skinned Mesh Renderer has to be an exposed entry.")]
            public SkinnedMeshRenderer skinnedMesh = null;
        }

        public class OutputProperties
        {
            [Tooltip("Outputs the number of vertices in the Mesh.")]
            public uint count;
        }

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("Specifies the kind of geometry to sample from.")]
        private SampleMesh.SourceType source = SampleMesh.SourceType.Mesh;

        protected sealed override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var props = base.inputProperties;
                if (source == SampleMesh.SourceType.Mesh)
                    props = props.Concat(PropertiesFromType(nameof(InputPropertiesMesh)));
                else if (source == SampleMesh.SourceType.SkinnedMeshRenderer)
                    props = props.Concat(PropertiesFromType(nameof(InputPropertiesSkinnedMeshRenderer)));
                else
                    throw new InvalidOperationException("Unexpected source type : " + source);
                return props;
            }
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var mesh = inputExpression[0].valueType == VFXValueType.Mesh ? inputExpression[0] : new VFXExpressionMeshFromSkinnedMeshRenderer(inputExpression[0]);
            var meshVertexCount = new VFXExpressionMeshVertexCount(mesh);
            return new VFXExpression[] { meshVertexCount };
        }
    }
}
