using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Slots;
using UnityEditor.ShaderGraph.Legacy;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class Texture3DInputMaterialSlot : Texture3DMaterialSlot
    {
        [SerializeField]
        int m_Version;

        [SerializeField]
        Texture3D m_Texture;

        public Texture3D texture
        {
            get => m_Texture;
            set => m_Texture = value;
        }

        public Texture3DInputMaterialSlot()
        {}

        public Texture3DInputMaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            ShaderStageCapability shaderStageCapability = ShaderStageCapability.All,
            bool hidden = false)
            : base(slotId, displayName, shaderOutputName, SlotType.Input, shaderStageCapability, hidden)
        {}

        public override VisualElement InstantiateControl()
        {
            return new Texture3DSlotControlView(this);
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

            var prop = new Texture3DShaderProperty();
            prop.overrideReferenceName = matOwner.GetVariableNameForSlot(id);
            prop.modifiable = false;
            prop.generatePropertyBlock = true;
            prop.value = texture;
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
                texture = slot.texture;
        }

        internal override void OnDeserialized(string json)
        {
            base.OnDeserialized(json);
            if (m_Version == 0)
            {
                m_Version = 1;
                m_Texture = (Texture3D)JsonUtility.FromJson<LegacyTexture>(json).texture;
            }
        }
    }
}
