using System;
using System.Text;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class Texture3DShaderProperty : AbstractShaderProperty<SerializableTexture>
    {
        public Texture3DShaderProperty()
        {
            displayName = "Texture3D";
            value = new SerializableTexture();
        }

#region Type
        public override PropertyType propertyType => PropertyType.Texture3D;
#endregion

#region Capabilities
        public override bool isBatchable => false;
        public override bool isExposable => true;
        public override bool isRenamable => true;
#endregion

#region PropertyBlock
        public string modifiableTagString => modifiable ? "" : "[NonModifiableTextureData]";

        public override string GetPropertyBlockString()
        {
            return $"{hideTagString}{modifiableTagString}[NoScaleOffset] {referenceName}(\"{displayName}\", 3D) = \"white\" {{}}";
        }
#endregion

#region ShaderValue
        public override string GetPropertyDeclarationString(string delimiter = ";")
        {
            return $"TEXTURE3D({referenceName}){delimiter} SAMPLER(sampler{referenceName}){delimiter}";
        }

        public override string GetPropertyAsArgumentString()
        {
            return $"TEXTURE3D_PARAM({referenceName}, sampler{referenceName})";
        }
#endregion

#region Options
        [SerializeField]
        private bool m_Modifiable = true;

        public bool modifiable
        {
            get => m_Modifiable;
            set => m_Modifiable = value;
        }
#endregion

#region Utility
        public override AbstractMaterialNode ToConcreteNode()
        {
            return new Texture3DAssetNode { texture = (Texture3D)value.texture };
        }

        public override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty(propertyType)
            {
                name = referenceName,
                textureValue = value.texture
            };
        }

        public override AbstractShaderProperty Copy()
        {
            var copied = new Texture3DShaderProperty();
            copied.displayName = displayName;
            copied.value = value;
            return copied;
        }
#endregion
    }
}
