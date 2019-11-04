using System;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    class SamplerStateShaderProperty : AbstractShaderProperty<TextureSamplerState>
    {
        public SamplerStateShaderProperty()
        {
            displayName = "SamplerState";
            value = new TextureSamplerState();
        }

        public override PropertyType propertyType => PropertyType.SamplerState;

        internal override bool isBatchable => false;
        internal override bool isExposable => false;
        internal override bool isRenamable => false;

        public override TextureSamplerState value
        {
            get => base.value;
            set
            {
                overrideReferenceName = $"{concreteShaderValueType.ToShaderString()}_{value.filter}_{value.wrap}";
                base.value = value;
            }
        }

        internal override string GetPropertyDeclarationString(string delimiter = ";")
        {
            return $"SAMPLER({referenceName}){delimiter}";
        }

        internal override string GetPropertyAsArgumentString()
        {
            return $"SamplerState {referenceName}";
        }

        internal override AbstractMaterialNode ToConcreteNode()
        {
            return new SamplerStateNode()
            {
                filter = value.filter,
                wrap = value.wrap
            };
        }

        internal override PreviewProperty GetPreviewMaterialProperty()
        {
            return default(PreviewProperty);
        }

        internal override ShaderInput Copy()
        {
            return new SamplerStateShaderProperty()
            {
                displayName = displayName,
                hidden = hidden,
                overrideReferenceName = overrideReferenceName,
                value = value
            };
        }
    }
}
