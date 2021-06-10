using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.Rendering;
using UnityEngine.Rendering.ShaderGraph;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    class SubGraphOutputNode : AbstractMaterialNode
    {
        static string s_MissingOutputSlot = "A Sub Graph must have at least one output slot";
        static List<ConcreteSlotValueType> s_ValidSlotTypes = new List<ConcreteSlotValueType>()
        {
            ConcreteSlotValueType.Vector1,
            ConcreteSlotValueType.Vector2,
            ConcreteSlotValueType.Vector3,
            ConcreteSlotValueType.Vector4,
            ConcreteSlotValueType.Matrix2,
            ConcreteSlotValueType.Matrix3,
            ConcreteSlotValueType.Matrix4,
            ConcreteSlotValueType.Boolean
        };
        public bool IsFirstSlotValid = true;

        public SubGraphOutputNode()
        {
            name = "Output";
        }

        // Link to the Sub Graph overview page instead of the specific Node page, seems more useful
        public override string documentationURL => Documentation.GetPageLink("Sub-graph");

        void ValidateShaderStage()
        {
            List<MaterialSlot> slots = new List<MaterialSlot>();
            GetInputSlots(slots);

            foreach (MaterialSlot slot in slots)
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

            foreach (MaterialSlot slot in slots)
                slot.stageCapability = effectiveStage;
        }

        void ValidateSlotName()
        {
            List<MaterialSlot> slots = new List<MaterialSlot>();
            GetInputSlots(slots);

            foreach (var slot in slots)
            {
                var error = NodeUtils.ValidateSlotName(slot.RawDisplayName(), out string errorMessage);
                if (error)
                {
                    owner.AddValidationError(objectId, errorMessage);
                    break;
                }
            }
        }

        void ValidateSlotType()
        {
            List<MaterialSlot> slots = new List<MaterialSlot>();
            GetInputSlots(slots);

            if (!slots.Any())
            {
                owner.AddValidationError(objectId, s_MissingOutputSlot, ShaderCompilerMessageSeverity.Error);
            }
            else if (!s_ValidSlotTypes.Contains(slots.FirstOrDefault().concreteValueType))
            {
                IsFirstSlotValid = false;
                owner.AddValidationError(objectId, "Preview can only compile if the first output slot is a Vector, Matrix, or Boolean type. Please adjust slot types.", ShaderCompilerMessageSeverity.Error);
            }
        }

        public override void ValidateNode()
        {
            base.ValidateNode();
            IsFirstSlotValid = true;
            ValidateSlotType();
            if (IsFirstSlotValid)
                ValidateShaderStage();
        }

        protected override void OnSlotsChanged()
        {
            base.OnSlotsChanged();
            ValidateNode();
        }

        public int AddSlot(ConcreteSlotValueType concreteValueType)
        {
            var index = this.GetInputSlots<MaterialSlot>().Count() + 1;
            var name = NodeUtils.GetDuplicateSafeNameForSlot(this, index, "Out_" + concreteValueType.ToString());
            AddSlot(MaterialSlot.CreateMaterialSlot(concreteValueType.ToSlotValueType(), index, name,
                NodeUtils.GetHLSLSafeName(name), SlotType.Input, Vector4.zero));
            return index;
        }

        public override bool canDeleteNode => false;

        public override bool canCopyNode => false;
    }
}
