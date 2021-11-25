using System.Collections.Generic;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Texture", "Texture 3D Asset")]
    [HasDependencies(typeof(Minimal2d3dTextureAssetNode))]
    class Texture3DAssetNode : AbstractMaterialNode, IPropertyFromNode
    {
        public const int OutputSlotId = 0;

        const string kOutputSlotName = "Out";

        public Texture3DAssetNode()
        {
            name = "Texture 3D Asset";
            synonyms = new string[] { "volume" };
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Texture3DMaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId });
        }

        [SerializeField]
        private SerializableTexture m_Texture = new SerializableTexture();

        [Texture3DControl("")]
        public Texture3D texture
        {
            get { return m_Texture.texture as Texture3D; }
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
            return base.GetVariableNameForSlot(OutputSlotId);
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return $"UnityBuildTexture3DStruct({GetTexturePropertyName()})";
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            properties.AddShaderProperty(new Texture3DShaderProperty()
            {
                overrideReferenceName = GetTexturePropertyName(),
                generatePropertyBlock = true,
                value = m_Texture,
                modifiable = false
            });
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            properties.Add(new PreviewProperty(PropertyType.Texture3D)
            {
                name = GetTexturePropertyName(),
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
