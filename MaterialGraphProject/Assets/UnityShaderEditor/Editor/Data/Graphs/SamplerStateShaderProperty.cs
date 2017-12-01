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
            return string.Format(@"
#ifdef UNITY_COMPILER_HLSL
SamplerState {0};
#endif", referenceName);
        }

        public override PreviewProperty GetPreviewMaterialProperty()
        {
            return null;
        }
    }
}
