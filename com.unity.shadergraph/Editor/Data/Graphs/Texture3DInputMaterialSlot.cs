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
    [HasDependencies(typeof(MinimalTexture3DInputMaterialSlot))]
    class Texture3DInputMaterialSlot : Texture3DMaterialSlot
    {
        [SerializeField]
        private SerializableTexture m_Texture = new SerializableTexture();

        public Texture texture
        {
            get { return m_Texture.texture; }
            set { m_Texture.texture = value; }
        }

        public override bool isDefaultValue => texture == null;

        public Texture3DInputMaterialSlot()
        { }

        public Texture3DInputMaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            ShaderStageCapability shaderStageCapability = ShaderStageCapability.All,
            bool hidden = false)
            : base(slotId, displayName, shaderOutputName, SlotType.Input, shaderStageCapability, hidden)
        { }

        public override VisualElement InstantiateControl()
        {
            return new Texture3DSlotControlView(this);
        }

        public override string GetDefaultValue(GenerationMode generationMode)
        {
            var nodeOwner = owner as AbstractMaterialNode;
            if (nodeOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractMaterialNode)));

            return $"UnityBuildTexture3DStruct({nodeOwner.GetVariableNameForSlot(id)})";
        }

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {
            var nodeOwner = owner as AbstractMaterialNode;
            if (nodeOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractMaterialNode)));

            var prop = new Texture3DShaderProperty();
            prop.overrideReferenceName = nodeOwner.GetVariableNameForSlot(id);
            prop.modifiable = false;
            prop.generatePropertyBlock = true;
            prop.value.texture = texture;
            properties.AddShaderProperty(prop);
        }

        public override void GetPreviewProperties(List<PreviewProperty> properties, string name)
        {
            var pp = new PreviewProperty(PropertyType.Texture3D)
            {
                name = name,
                textureValue = texture,
            };
            properties.Add(pp);
        }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
            var slot = foundSlot as Texture3DInputMaterialSlot;
            if (slot != null)
            {
                m_Texture = slot.m_Texture;
                bareResource = slot.bareResource;
            }
        }
    }

    class MinimalTexture3DInputMaterialSlot : IHasDependencies
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
