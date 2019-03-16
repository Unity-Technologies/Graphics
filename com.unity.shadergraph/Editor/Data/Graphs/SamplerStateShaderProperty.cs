using System;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    class SamplerStateShaderProperty : AbstractShaderProperty<TextureSamplerState>
    {
        public SamplerStateShaderProperty()
        {
            displayName = "SamplerState";

            if(value == null)
                value = new TextureSamplerState();
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.SamplerState; }
        }

        public override Vector4 defaultValue
        {
            get { return new Vector4(); }
        }

        public override bool isBatchable
        {
            get { return false; }
        }

        public override bool isExposable
        {
            get { return false; }
        }

        public override string GetPropertyBlockString()
        {
            return string.Empty;
        }

        public override string GetPropertyDeclarationString(string delimiter = ";")
        {
            return string.Format(@"SAMPLER({0}_{1}_{2}){3}", referenceName, 
                Enum.GetName(typeof(TextureSamplerState.FilterMode), value.filter), 
                Enum.GetName(typeof(TextureSamplerState.WrapMode), value.wrap), 
                delimiter);
        }

        public override string GetPropertyAsArgumentString()
        {
            return string.Format(@"SamplerState {0}_{1}_{2}", referenceName, 
                Enum.GetName(typeof(TextureSamplerState.FilterMode), value.filter), 
                Enum.GetName(typeof(TextureSamplerState.WrapMode), value.wrap));
        }

        public override PreviewProperty GetPreviewMaterialProperty()
        {
            return default(PreviewProperty);
        }

        public override AbstractMaterialNode ToConcreteNode()
        {
            return new SamplerStateNode() 
            {
                filter = value.filter,
                wrap = value.wrap
            };
        }

        public override AbstractShaderProperty Copy()
        {
            var copied = new SamplerStateShaderProperty();
            copied.displayName = displayName;
            copied.value = value;
            return copied;
        }
    }
}
