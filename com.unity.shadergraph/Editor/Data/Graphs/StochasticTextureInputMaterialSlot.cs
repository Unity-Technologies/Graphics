using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Slots;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    [HasDependencies(typeof(MinimalTexture2DInputMaterialSlot))]
    class StochasticTextureInputMaterialSlot : StochasticTextureMaterialSlot
    {
        [SerializeField]
        private SerializableStochasticTexture m_StochasticTexture = new SerializableStochasticTexture();

        public ProceduralTexture2D texture
        {
            get { return m_StochasticTexture.proceduralTexture; }
            set { m_StochasticTexture.proceduralTexture = value; }
        }

        public override bool isDefaultValue => texture == null;

        public StochasticTextureInputMaterialSlot()
        {}

        public StochasticTextureInputMaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            ShaderStageCapability stageCapability = ShaderStageCapability.All,
            bool hidden = false)
            : base(slotId, displayName, shaderOutputName, SlotType.Input, stageCapability, hidden)
        {}

        public override VisualElement InstantiateControl()
        {
            var view = new ProceduralTexture2DSlotControlView(this);
            return view;
        }

        public override string GetDefaultValue(GenerationMode generationMode)
        {
            var nodeOwner = owner as AbstractMaterialNode;
            if (nodeOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractMaterialNode)));

            return $"UnityBuildStochasticTexture2DStruct({nodeOwner.GetVariableNameForSlot(id)})";
        }

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {
            var prop = CreateProperty();
            properties.AddShaderProperty(prop);
        }

        StochasticTextureShaderProperty CreateProperty()
        {
            var nodeOwner = owner as AbstractMaterialNode;
            if (nodeOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractMaterialNode)));

            var prop = new StochasticTextureShaderProperty();
            prop.overrideReferenceName = nodeOwner.GetVariableNameForSlot(id);
            prop.generatePropertyBlock = true;
            prop.value.proceduralTexture = texture;
            return prop;
        }

        public override void GetPreviewProperties(List<PreviewProperty> properties, string name)
        {
            var prop = CreateProperty();
            var pp = new PreviewProperty(PropertyType.StochasticTexture)
            {
                name = name,
                stochasticProperty = prop,
            };
            properties.Add(pp);
        }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
            var slot = foundSlot as StochasticTextureInputMaterialSlot;
            if (slot != null)
            {
                m_StochasticTexture = slot.m_StochasticTexture;
                bareResource = slot.bareResource;
            }
        }
    }
/*
    class MinimalTexture2DInputMaterialSlot : IHasDependencies
    {
        [SerializeField]
        private SerializableTexture m_Texture = null;

        public void GetSourceAssetDependencies(AssetCollection assetCollection)
        {
            var guidString = m_Texture.guid;
            if (!string.IsNullOrEmpty(guidString) && GUID.TryParse(guidString, out var guid))
            {
                assetCollection.AddAssetDependency(guid, AssetCollection.Flags.IncludeInExportPackage);
            }
        }
    }
*/
}
