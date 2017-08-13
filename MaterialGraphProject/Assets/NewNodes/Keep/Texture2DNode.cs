using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Input/Texture/Texture Asset")]
    public class Texture2DNode : PropertyNode
    {
        protected const string textureName = "Texture";

        public const int textureID = 0;

        [SerializeField]
        private string m_SerializedTexture;

        [SerializeField]
        private TextureType m_TextureType;

        [Serializable]
        private class TextureHelper
        {
            public Texture texture;
        }

        public override bool hasPreview { get { return false; } }

#if UNITY_EDITOR
        public Texture defaultTexture
        {
            get
            {
                if (string.IsNullOrEmpty(m_SerializedTexture))
                    return null;

                var tex = new TextureHelper();
                EditorJsonUtility.FromJsonOverwrite(m_SerializedTexture, tex);
                return tex.texture;
            }
            set
            {
                if (defaultTexture == value)
                    return;

                var tex = new TextureHelper();
                tex.texture = value;
                m_SerializedTexture = EditorJsonUtility.ToJson(tex, true);

                if (onModified != null)
                {
                    onModified(this, ModificationScope.Node);
                }
            }
        }
#else
        public Texture defaultTexture {get; set; }
#endif

        public TextureType textureType
        {
            get { return m_TextureType; }
            set
            {
                if (m_TextureType == value)
                    return;


                m_TextureType = value;
                if (onModified != null)
                {
                    onModified(this, ModificationScope.Graph);
                }
            }
        }

        public Texture2DNode()
        {
            name = "Texture2D";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new MaterialSlot(textureID, textureName, textureName, SlotType.Output, SlotValueType.Texture2D, Vector4.zero));
            RemoveSlotsNameNotMatching(validSlots);
        }

        protected int[] validSlots
        {
            get { return new[] { textureID }; }
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            properties.Add(GetPreviewProperty());
        }

        // Properties
        public override void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
        {
            visitor.AddShaderProperty(
                new TexturePropertyChunk(
                    propertyName,
                    description,
                    defaultTexture, m_TextureType,
                    PropertyChunk.HideState.Visible,
                    exposedState == ExposedState.Exposed ?
                        TexturePropertyChunk.ModifiableState.Modifiable
                        : TexturePropertyChunk.ModifiableState.NonModifiable));
        }


        public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var slotTexture2D = FindOutputSlot<MaterialSlot>(0);
            if (slotTexture2D != null)
            {
                visitor.AddShaderChunk("UNITY_DECLARE_TEX2D(" + propertyName + ");", true);
            }
        }


        public override PreviewProperty GetPreviewProperty()
        {
            return new PreviewProperty
            {
                m_Name = propertyName,
                m_PropType = PropertyType.Texture,
                m_Texture = defaultTexture
            };
        }

        public override PropertyType propertyType { get { return PropertyType.Texture; } }

    }
}
