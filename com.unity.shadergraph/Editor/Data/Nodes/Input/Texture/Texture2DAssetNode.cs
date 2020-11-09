using System.Collections.Generic;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Texture", "Texture 2D Asset")]
    class Texture2DAssetNode : AbstractMaterialNode, IPropertyFromNode
    {
        public const int OutputSlotId = 0;

        const string kOutputSlotName = "Out";

        public Texture2DAssetNode()
        {
            name = "Texture 2D Asset";
            UpdateNodeAfterDeserialization();
        }


        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Texture2DMaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId });
        }

        [SerializeField]
        private SerializableTexture m_Texture = new SerializableTexture();

        [TextureControl("")]
        public Texture texture
        {
            get { return m_Texture.texture; }
            set
            {
                if (m_Texture.texture == value)
                    return;
                m_Texture.texture = value;
                Dirty(ModificationScope.Node);
            }
        }

        string GetTexturePropertyName()
        {
            return string.Format("_{0}_texture", GetVariableNameForNode());
        }

        string GetTextureVariableName()
        {
            return string.Format("_{0}_texture_struct", GetVariableNameForNode());
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            if (slotId == OutputSlotId)
                return GetTextureVariableName();
            else
                return base.GetVariableNameForSlot(slotId);
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            properties.AddShaderProperty(new Texture2DShaderProperty()
            {
                // NOTE : this changes (hidden) shader property names... which could cause Material changes
                overrideReferenceName = GetTexturePropertyName(),
                generatePropertyBlock = true,
                value = m_Texture,
                modifiable = false
            });
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            properties.Add(new PreviewProperty(PropertyType.Texture2D)
            {
                name = GetTexturePropertyName(),
                textureValue = texture,
                texture2DDefaultType = Texture2DShaderProperty.DefaultType.White
            });
        }

        public AbstractShaderProperty AsShaderProperty()
        {
            var prop = new Texture2DShaderProperty { value = m_Texture };
            if (texture != null)
                prop.displayName = texture.name;
            return prop;
        }

        public int outputSlotId { get { return OutputSlotId; } }
    }
}
