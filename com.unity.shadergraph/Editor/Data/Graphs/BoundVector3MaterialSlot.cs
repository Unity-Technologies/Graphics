using System;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Slots;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class BoundVector3MaterialSlot : SpaceMaterialSlot
    {
        [SerializeField]
        private CoordinateSpace m_Space;

        public BoundVector3MaterialSlot()
        {
        }

        public BoundVector3MaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            Vector3 value,
            CoordinateSpace space,
            ShaderStage shaderStage = ShaderStage.Dynamic,
            bool hidden = false)
            : base(slotId, displayName, shaderOutputName, space, shaderStage, hidden)
        {
            this.value = value;
        }

        public override VisualElement InstantiateControl()
        {
            return new LabelSlotControlView(space + " Space");
        }

        protected override string ConcreteSlotValueAsVariable(AbstractMaterialNode.OutputPrecision precision)
        {
            return precision + String.Format("3 ({0}, {1}, {2})", NodeUtils.FloatToShaderValue(value.x), NodeUtils.FloatToShaderValue(value.y), NodeUtils.FloatToShaderValue(value.z));
        }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
            var slot = foundSlot as BoundVector3MaterialSlot;
            if (slot != null)
                value = slot.value;
        }
    }
}
