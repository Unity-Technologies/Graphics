using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Operator
{
    class SampleMeshTriangleCountProvider : SampleMeshProvider
    {
        protected override string nameTemplate { get; } = "Get {0} Triangle Count";
        protected override Type operatorType { get; } = typeof(MeshTriangleCount);
    }

    //[VFXHelpURL("Operator-MeshTriangleCount")]
    [VFXInfo(variantProvider = typeof(SampleMeshTriangleCountProvider))]
    class MeshTriangleCount : VFXOperator
    {
        override public string name
        {
            get
            {
                if (source == SampleMesh.SourceType.Mesh)
                    return "Get Mesh Triangle Count";
                else
                    return "Get Skinned Mesh Triangle Count";
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
            [Tooltip("The number of triangle in this mesh")]
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
            var meshIndexCount = new VFXExpressionMeshIndexCount(mesh);
            return new VFXExpression[] { meshIndexCount / VFXValue.Constant(3u) };
        }
    }
}
