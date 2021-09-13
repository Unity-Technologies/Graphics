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
    class Texture2DInputMaterialSlot : Texture2DMaterialSlot
    {
        [SerializeField]
        private SerializableTexture m_Texture = new SerializableTexture();

        [SerializeField]
        private Texture2DShaderProperty.DefaultType m_DefaultType = Texture2DShaderProperty.DefaultType.White;

        public Texture texture
        {
            get { return m_Texture.texture; }
            set { m_Texture.texture = value; }
        }

        public Texture2DShaderProperty.DefaultType defaultType
        {
            get { return m_DefaultType; }
            set { m_DefaultType = value; }
        }

        public override bool isDefaultValue => texture == null;

        public Texture2DInputMaterialSlot()
        { }

        public Texture2DInputMaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            ShaderStageCapability stageCapability = ShaderStageCapability.All,
            bool hidden = false)
            : base(slotId, displayName, shaderOutputName, SlotType.Input, stageCapability, hidden)
        { }

        public override VisualElement InstantiateControl()
        {
            return new TextureSlotControlView(this);
        }

        public override string GetDefaultValue(GenerationMode generationMode)
        {
            var nodeOwner = owner as AbstractMaterialNode;
            if (nodeOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractMaterialNode)));

            return $"UnityBuildTexture2DStructNoScale({nodeOwner.GetVariableNameForSlot(id)})";
        }

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {
            var nodeOwner = owner as AbstractMaterialNode;
            if (nodeOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractMaterialNode)));

            var prop = new Texture2DShaderProperty();
            prop.overrideReferenceName = nodeOwner.GetVariableNameForSlot(id);
            prop.modifiable = false;
            prop.generatePropertyBlock = true;
            prop.value.texture = texture;
            prop.defaultType = defaultType;
            properties.AddShaderProperty(prop);
        }

        public override void GetPreviewProperties(List<PreviewProperty> properties, string name)
        {
            var pp = new PreviewProperty(PropertyType.Texture2D)
            {
                name = name,
                textureValue = texture,
                texture2DDefaultType = defaultType
            };
            properties.Add(pp);
        }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
            var slot = foundSlot as Texture2DInputMaterialSlot;
            if (slot != null)
            {
                m_Texture = slot.m_Texture;
                bareResource = slot.bareResource;
            }
        }
    }

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
}
