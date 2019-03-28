using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    class SubGraphOutputNode : AbstractMaterialNode, IHasSettings
    {
        static string s_MissingOutputSlot = "A Sub Graph must have at least one output slot";

        public SubGraphOutputNode()
        {
            name = "Output";
        }

        void ValidateShaderStage()
            {
                List<MaterialSlot> slots = new List<MaterialSlot>();
                GetInputSlots(slots);

                foreach(MaterialSlot slot in slots)
                slot.stageCapability = ShaderStageCapability.All;

            var effectiveStage = ShaderStageCapability.All;
            foreach (var slot in slots)
                {
                var stage = NodeUtils.GetEffectiveShaderStageCapability(slot, true);
                if (stage != ShaderStageCapability.All)
                {
                    effectiveStage = stage;
                    break;
            }
        }

            foreach(MaterialSlot slot in slots)
                slot.stageCapability = effectiveStage;
        }

        public override void ValidateNode()
        {
            ValidateShaderStage();

            if (!this.GetInputSlots<MaterialSlot>().Any())
            {
                owner.AddValidationError(tempId, s_MissingOutputSlot, ShaderCompilerMessageSeverity.Warning);
            }
            
            base.ValidateNode();
        }

        public int AddSlot(ConcreteSlotValueType concreteValueType)
        {
            var index = this.GetInputSlots<ISlot>().Count() + 1;
            string name = string.Format("Out_{0}", NodeUtils.GetDuplicateSafeNameForSlot(this, index, concreteValueType.ToString()));
            AddSlot(MaterialSlot.CreateMaterialSlot(concreteValueType.ToSlotValueType(), index, name, NodeUtils.GetHLSLSafeName(name), SlotType.Input, Vector4.zero));
            return index;
        }

        protected override void OnSlotsChanged()
        {
            base.OnSlotsChanged();
            ValidateNode();
        }

        static ConcreteSlotValueType[] s_AllowedValueTypes =
        {
            ConcreteSlotValueType.Matrix4,
            ConcreteSlotValueType.Matrix3,
            ConcreteSlotValueType.Matrix2,
            ConcreteSlotValueType.Gradient,
            ConcreteSlotValueType.Vector4,
            ConcreteSlotValueType.Vector3,
            ConcreteSlotValueType.Vector2,
            ConcreteSlotValueType.Vector1,
            ConcreteSlotValueType.Boolean
        };

        public VisualElement CreateSettingsElement()
        {
            PropertySheet ps = new PropertySheet();
            ps.Add(new ReorderableSlotListView(this, SlotType.Input, s_AllowedValueTypes));
            return ps;
        }
    }
}
