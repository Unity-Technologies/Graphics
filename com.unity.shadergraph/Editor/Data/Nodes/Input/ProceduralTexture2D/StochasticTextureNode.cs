using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Texture", "Stochastic Texture")]
    class StochasticTextureNode : AbstractMaterialNode, IPropertyFromNode
    {
        public StochasticTextureNode()
        {
            name = "Stochastic Texture";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            // This still needs to be added.
            get { return ""; }
        }

        [SerializeField]
        ProceduralTexture2D m_ProceduralTexture2DAsset;

        [ObjectControl]
        public ProceduralTexture2D proceduralTexture2D
        {
            get => m_ProceduralTexture2DAsset;
            set
            {
                if (m_ProceduralTexture2DAsset == value)
                    return;
                m_ProceduralTexture2DAsset = value;
                Dirty(ModificationScope.Node);
            }
        }

        private const int kOutputSlotId = 0;
        private const string kOutputSlotName = "Out";

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new StochasticTextureMaterialSlot(kOutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotId });
        }

        string GetTexturePropertyName()
        {
            return base.GetVariableNameForSlot(kOutputSlotId);
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return $"UnityBuildStochasticTexture2DStruct({GetTexturePropertyName()})";
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            var serializableST = new SerializableStochasticTexture();
            serializableST.proceduralTexture = proceduralTexture2D;

            properties.AddShaderProperty(new StochasticTextureShaderProperty()
            {
                overrideReferenceName = GetTexturePropertyName(),
                generatePropertyBlock = true,
                value = serializableST,
                // modifiable = false
            });
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            var stochasticProperty = new StochasticTextureShaderProperty();
            stochasticProperty.value.proceduralTexture = proceduralTexture2D;

            properties.Add(new PreviewProperty(PropertyType.StochasticTexture)
            {
                name = GetTexturePropertyName(),
                stochasticProperty = stochasticProperty,
            });
        }

        public AbstractShaderProperty AsShaderProperty()
        {
            var serializableST = new SerializableStochasticTexture();
            serializableST.proceduralTexture = proceduralTexture2D;

            var prop = new StochasticTextureShaderProperty { value = serializableST };
            if (proceduralTexture2D != null)
                prop.displayName = proceduralTexture2D.name;
            return prop;
        }

        public int outputSlotId { get { return kOutputSlotId; } }

    }
}
