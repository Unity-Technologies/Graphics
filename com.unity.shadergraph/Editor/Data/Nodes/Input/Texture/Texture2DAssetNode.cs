using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Serialization;

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

        Texture m_Texture;

        [TextureControl("")]
        [JsonProperty]
        [JsonUpgrade("m_Texture", typeof(SerializableTextureUpgrader))]
        public Texture texture
        {
            get => m_Texture;
            set
            {
                if (m_Texture == value)
                    return;
                m_Texture = value;
                Dirty(ModificationScope.Node);
            }
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            properties.AddShaderProperty(new TextureShaderProperty()
            {
                overrideReferenceName = GetVariableNameForSlot(OutputSlotId),
                generatePropertyBlock = true,
                value = m_Texture,
                modifiable = false
            });
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            properties.Add(new PreviewProperty(PropertyType.Texture2D)
            {
                name = GetVariableNameForSlot(OutputSlotId),
                textureValue = texture
            });
        }

        public AbstractShaderProperty AsShaderProperty()
        {
            var prop = new TextureShaderProperty { value = m_Texture };
            if (texture != null)
                prop.displayName = texture.name;
            return prop;
        }

        public int outputSlotId { get { return OutputSlotId; } }
    }
}
