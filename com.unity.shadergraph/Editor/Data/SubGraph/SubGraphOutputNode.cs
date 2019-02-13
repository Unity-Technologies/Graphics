using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    class SubGraphOutputNode : AbstractMaterialNode, IHasSettings
    {
        public SubGraphOutputNode()
        {
            name = "Output";
        }

        public ShaderStageCapability effectiveShaderStage
        {
            get
            {
                List<MaterialSlot> slots = new List<MaterialSlot>();
                GetInputSlots(slots);

                foreach(MaterialSlot slot in slots)
                {
                    ShaderStageCapability stage = NodeUtils.GetEffectiveShaderStageCapability(slot, true);

                    if(stage != ShaderStageCapability.All)
                        return stage;
                }

                return ShaderStageCapability.All;
            }
        }

        private void ValidateShaderStage()
        {
            List<MaterialSlot> slots = new List<MaterialSlot>();
            GetInputSlots(slots);

            foreach(MaterialSlot slot in slots)
                slot.stageCapability = ShaderStageCapability.All;

            var effectiveStage = effectiveShaderStage;

            foreach(MaterialSlot slot in slots)
                slot.stageCapability = effectiveStage;
        }

        public override void ValidateNode()
        {
            ValidateShaderStage();

            base.ValidateNode();
        }

        public virtual int AddSlot()
        {
            var index = this.GetInputSlots<ISlot>().Count() + 1;
            AddSlot(new Vector4MaterialSlot(index, "Output " + index, "Output" + index, SlotType.Input, Vector4.zero));
            return index;
        }

        public void RemapOutputs(ShaderGenerator visitor, GenerationMode generationMode)
        {
            foreach (var slot in graphOutputs)
                visitor.AddShaderChunk(string.Format("{0} = {1};", slot.shaderOutputName, GetSlotValue(slot.id, generationMode)), true);
        }

        public IEnumerable<MaterialSlot> graphOutputs
        {
            get
            {
                return NodeExtensions.GetInputSlots<MaterialSlot>(this).OrderBy(x => x.id);
            }
        }

        public VisualElement CreateSettingsElement()
        {
            PropertySheet ps = new PropertySheet();
            ps.Add(new ShaderValueDescriptorListView(this, SlotType.Input));
            return ps;
        }
    }
}
