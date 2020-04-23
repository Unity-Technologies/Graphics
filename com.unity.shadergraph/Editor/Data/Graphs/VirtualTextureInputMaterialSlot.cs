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
    class VirtualTextureInputMaterialSlot : VirtualTextureMaterialSlot
    {
        internal VirtualTextureShaderProperty m_Default;

        public VirtualTextureInputMaterialSlot()
        {
            m_Default = new VirtualTextureShaderProperty();
        }

        public VirtualTextureInputMaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            ShaderStageCapability stageCapability = ShaderStageCapability.All,
            bool hidden = false)
            : base(slotId, displayName, shaderOutputName, SlotType.Input, stageCapability, hidden)
        {
            m_Default = new VirtualTextureShaderProperty();
        }

        public override VisualElement InstantiateControl()
        {
            return new VirtualTextureSlotControlView(this);
        }

        public override string GetDefaultValue(GenerationMode generationMode)
        {
            var matOwner = owner as AbstractMaterialNode;
            if (matOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractMaterialNode)));

            return matOwner.GetVariableNameForSlot(id);
        }

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {
            var matOwner = owner as AbstractMaterialNode;
            if (matOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractMaterialNode)));

            VirtualTextureShaderProperty prop = m_Default.Copy() as VirtualTextureShaderProperty;
            prop.overrideReferenceName = matOwner.GetVariableNameForSlot(id);
            // prop.modifiable = false;
            prop.generatePropertyBlock = true;
            // prop.value.texture = texture;
            // prop.defaultType = defaultType;
            properties.AddShaderProperty(prop);
        }

        public override void GetPreviewProperties(List<PreviewProperty> properties, string name)
        {
            var pp = new PreviewProperty(PropertyType.VirtualTexture)
            {
                name = name,
                // textureValue = texture,      // TODO: virtual textures pass what...   strings?   nothing?
            };
            properties.Add(pp);
        }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
            Debug.Log("Copy Values From");
            // TODO
//             var slot = foundSlot as Texture2DInputMaterialSlot;
//             if (slot != null)
//                 m_Texture = slot.m_Texture;
        }
    }
}
