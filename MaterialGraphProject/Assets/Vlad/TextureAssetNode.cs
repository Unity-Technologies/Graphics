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
    public class TextureAssetNode : PropertyNode
    {
        protected const string outputTexture2D_name = "Texture2D";
        protected const string outputSampler2D_name = "Sampler2D";

        public const int outputTexture2D_id = 0;
        public const int outputSampler2D_id = 1;

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

        public TextureAssetNode()
        {
            name = "TextureAsset";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new MaterialSlot(outputTexture2D_id, outputTexture2D_name, outputTexture2D_name, SlotType.Output, SlotValueType.Texture2D, Vector4.zero, false));
            AddSlot(new MaterialSlot(outputSampler2D_id, outputSampler2D_name, outputSampler2D_name, SlotType.Output, SlotValueType.Sampler2D, Vector4.zero, false));
            RemoveSlotsNameNotMatching(validSlots);
        }

        protected int[] validSlots
        {
            get { return new[] { outputTexture2D_id, outputSampler2D_id }; }
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
                var edgesTexture2D = owner.GetEdges(slotTexture2D.slotReference).ToList();
                if (edgesTexture2D.Count > 0)
                {
                    visitor.AddShaderChunk("#ifdef UNITY_COMPILER_HLSL", true);
                    visitor.AddShaderChunk("Texture2D " + propertyName + ";", true);
                    visitor.AddShaderChunk("#endif", true);
                }
            }

            var slotSampler2D = FindOutputSlot<MaterialSlot>(1);
            if (slotSampler2D != null)
            {
                var edgesSampler2D = owner.GetEdges(slotSampler2D.slotReference).ToList();
                if (edgesSampler2D.Count > 0)
                {
                    visitor.AddShaderChunk("sampler2D " + propertyName + ";", true);
                }
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
