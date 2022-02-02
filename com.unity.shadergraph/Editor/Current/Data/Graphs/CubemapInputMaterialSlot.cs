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
    [HasDependencies(typeof(MinimalCubemapInputMaterialSlot))]
    class CubemapInputMaterialSlot : CubemapMaterialSlot
    {
        [SerializeField]
        private SerializableCubemap m_Cubemap = new SerializableCubemap();

        public Cubemap cubemap
        {
            get { return m_Cubemap.cubemap; }
            set { m_Cubemap.cubemap = value; }
        }

        public override bool isDefaultValue => cubemap == null;

        public CubemapInputMaterialSlot()
        { }

        public CubemapInputMaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            ShaderStageCapability stageCapability = ShaderStageCapability.All,
            bool hidden = false)
            : base(slotId, displayName, shaderOutputName, SlotType.Input, stageCapability, hidden)
        { }

        public override VisualElement InstantiateControl()
        {
            return new CubemapSlotControlView(this);
        }

        public override string GetDefaultValue(GenerationMode generationMode)
        {
            var nodeOwner = owner as AbstractMaterialNode;
            if (nodeOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractMaterialNode)));

            return $"UnityBuildTextureCubeStruct({nodeOwner.GetVariableNameForSlot(id)})";
        }

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {
            var nodeOwner = owner as AbstractMaterialNode;
            if (nodeOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractMaterialNode)));

            var prop = new CubemapShaderProperty();
            prop.overrideReferenceName = nodeOwner.GetVariableNameForSlot(id);
            prop.modifiable = false;
            prop.generatePropertyBlock = true;
            prop.value.cubemap = cubemap;
            properties.AddShaderProperty(prop);
        }

        public override void GetPreviewProperties(List<PreviewProperty> properties, string name)
        {
            var pp = new PreviewProperty(PropertyType.Cubemap)
            {
                name = name,
                cubemapValue = cubemap
            };
            properties.Add(pp);
        }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
            var slot = foundSlot as CubemapInputMaterialSlot;
            if (slot != null)
            {
                m_Cubemap = slot.m_Cubemap;
                bareResource = slot.bareResource;
            }
        }
    }

    class MinimalCubemapInputMaterialSlot : IHasDependencies
    {
        [SerializeField]
        private SerializableCubemap m_Cubemap = null;

        public void GetSourceAssetDependencies(AssetCollection assetCollection)
        {
            var guidString = m_Cubemap.guid;
            if (!string.IsNullOrEmpty(guidString) && GUID.TryParse(guidString, out var guid))
            {
                assetCollection.AddAssetDependency(guid, AssetCollection.Flags.IncludeInExportPackage);
            }
        }
    }
}
