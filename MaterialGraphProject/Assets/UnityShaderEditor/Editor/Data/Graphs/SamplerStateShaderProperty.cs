using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    class SamplerStateShaderProperty : AbstractShaderProperty<TextureSamplerState>
    {
        public override PropertyType propertyType
        {
            get { return PropertyType.SamplerState; }
        }

        public override Vector4 defaultValue
        {
            get { return new Vector4(); }
        }

        public override string GetPropertyBlockString()
        {
            return string.Empty;
        }

        public override string GetPropertyDeclarationString()
        {
            string ss = referenceName + "_"
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
