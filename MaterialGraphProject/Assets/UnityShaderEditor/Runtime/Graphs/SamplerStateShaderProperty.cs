using System;

namespace UnityEngine.MaterialGraph
{
    class SamplerStateShaderProperty : AbstractShaderProperty<TextureSamplerState>
    {
        public override PropertyType propertyType
        {
            get { return PropertyType.SamplerState; }
        }

        public override string GetPropertyBlockString()
        {
            return string.Empty;
        }

        public override string GetPropertyDeclarationString()
        {
            string ss = name + "_"
                        + Enum.GetName(typeof(TextureSamplerState.FilterMode), value.filter) + "_"
                        + Enum.GetName(typeof(TextureSamplerState.WrapMode), value.wrap) + "_sampler;";

            return string.Format(@"
#ifdef UNITY_COMPILER_HLSL
SamplerState {0};
#endif", ss);
        }

        public override PreviewProperty GetPreviewMaterialProperty()
        {
            return null;
        }
    }
}
