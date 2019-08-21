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
                overrideReferenceName = GetBuiltinSamplerName(value.filter, value.wrap);
                base.value = value;
            }
        }
        
        public static string GetBuiltinSamplerName(TextureSamplerState.FilterMode filterMode, TextureSamplerState.WrapMode wrapMode)
            => $"{PropertyType.SamplerState.ToConcreteShaderValueType().ToShaderString()}_{filterMode}_{wrapMode}";

        public override string GetPropertyAsArgumentString()
        {
            return $"SAMPLER({referenceName})";
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
