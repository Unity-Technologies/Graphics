using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Serialization;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Texture", "Texture 3D Asset")]
    class Texture3DAssetNode : AbstractMaterialNode, IPropertyFromNode
    {
        public const int OutputSlotId = 0;

        const string kOutputSlotName = "Out";

        public Texture3DAssetNode()
        {
            name = "Texture 3D Asset";
            UpdateNodeAfterDeserialization();
        }


        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Texture3DMaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId });
        }

        Texture3D m_Texture;

        [Texture3DControl("")]
        [JsonProperty]
        // TODO: Test that this works
        [JsonUpgrade("m_Texture", typeof(SerializableTextureUpgrader))]
        public Texture3D texture
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
            properties.AddShaderProperty(new Texture3DShaderProperty()
            {
                overrideReferenceName = GetVariableNameForSlot(OutputSlotId),
                generatePropertyBlock = true,
                value = m_Texture,
                modifiable = false
            });
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            properties.Add(new PreviewProperty(PropertyType.Texture3D)
            {
                name = GetVariableNameForSlot(OutputSlotId),
                textureValue = texture
            });
        }

        public AbstractShaderProperty AsShaderProperty()
        {
            var prop = new Texture3DShaderProperty { value = m_Texture };
            if (texture != null)
                prop.displayName = texture.name;
            return prop;
        }

        public int outputSlotId { get { return OutputSlotId; } }
    }
}
