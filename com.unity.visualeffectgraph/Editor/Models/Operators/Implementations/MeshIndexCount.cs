using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Operator
{
    [VFXInfo(category = "Sampling", variantProvider = typeof(SampleMeshProvider), experimental = true)]
    class MeshIndexCount : VFXOperator
    {
        override public string name
        {
            get
            {
                if (source == SampleMesh.SourceType.Mesh)
                    return "Mesh Index Count";
                else
                    return "Skinned Mesh Renderer Index Count";
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

        public class OutputProperties
        {
            [Tooltip("The number of indices in this mesh")]
            public uint count;
        }

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("Choose between source from mesh or skinned renderer mesh.")]
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
            return new VFXExpression[] { meshIndexCount };
        }
    }
}
