using System;
using System.Text;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class Texture2DArrayShaderProperty : AbstractShaderProperty<SerializableTextureArray>
    {
        public Texture2DArrayShaderProperty()
        {
            displayName = "Texture2D Array";
            value = new SerializableTextureArray();
        }

#region Type
        public override PropertyType propertyType => PropertyType.Texture2DArray;
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
            return $"{hideTagString}{modifiableTagString}[NoScaleOffset] {referenceName}(\"{displayName}\", 2DArray) = \"white\" {{}}";
        }
#endregion

#region ShaderValue
        public override string GetPropertyDeclarationString(string delimiter = ";")
        {
            return $"TEXTURE2D_ARRAY({referenceName}){delimiter} SAMPLER(sampler{referenceName}){delimiter}";
        }

        public override string GetPropertyAsArgumentString()
        {
            return $"TEXTURE2D_ARRAY_PARAM({referenceName}, sampler{referenceName})";
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
            return new Texture2DArrayAssetNode { texture = value.textureArray };
        }

        
        public override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty(propertyType)
            {
                name = referenceName,
                textureValue = value.textureArray
            };
        }

        public override AbstractShaderProperty Copy()
        {
            var copied = new Texture2DArrayShaderProperty();
            copied.displayName = displayName;
            copied.value = value;
            return copied;
        }
#endregion
    }
}
