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
        public VirtualTextureInputMaterialSlot()
        {
        }

        public VirtualTextureInputMaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            ShaderStageCapability stageCapability = ShaderStageCapability.All,
            bool hidden = false)
            : base(slotId, displayName, shaderOutputName, SlotType.Input, stageCapability, hidden)
        {
        }

        public override VisualElement InstantiateControl()
        {
            return null;
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
        }

        public override void GetPreviewProperties(List<PreviewProperty> properties, string name)
        {
        }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
        }
    }
}
