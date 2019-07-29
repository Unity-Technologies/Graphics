using System.Collections.Generic;

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
        
        public override bool isExposable => false;
        public override bool isRenamable => false;

        public override TextureSamplerState value
        {
            get => base.value;
            set
            {
                overrideReferenceName = $"{concreteShaderValueType.ToShaderString()}_{value.filter}_{value.wrap}";
                base.value = value;
            }
        }

        public override IEnumerable<(string cbName, string line)> GetPropertyDeclarationStrings()
        {
            yield return (null, $"SAMPLER({referenceName})");
        }

        public override string GetPropertyAsArgumentString()
        {
            return $"SamplerState {referenceName}";
        }
        
        public override AbstractMaterialNode ToConcreteNode()
        {
            return new SamplerStateNode() 
            {
                filter = value.filter,
                wrap = value.wrap
            };
        }

        public override PreviewProperty GetPreviewMaterialProperty()
        {
            return default(PreviewProperty);
        }

        public override ShaderInput Copy()
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
