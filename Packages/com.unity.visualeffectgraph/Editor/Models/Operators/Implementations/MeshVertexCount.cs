using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Operator
{
    class SampleMeshVertexCountProvider : SampleMeshProvider
    {
        protected override string nameTemplate { get; } = "Get {0} Vertex Count";
        protected override Type operatorType { get; } = typeof(MeshVertexCount);
    }

    [VFXHelpURL("Operator-MeshVertexCount")]
    [VFXInfo(variantProvider = typeof(SampleMeshVertexCountProvider))]
    class MeshVertexCount : VFXOperator
    {
        override public string name
        {
            get
            {
                if (source == SampleMesh.SourceType.Mesh)
                    return "Get Mesh Vertex Count";
                else
                    return "Get Skinned Mesh Vertex Count";
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
