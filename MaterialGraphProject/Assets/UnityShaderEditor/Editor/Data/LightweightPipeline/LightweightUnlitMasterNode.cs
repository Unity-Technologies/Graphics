using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    [Title("Master/Lightweight/Unlit")]
    public class LightweightUnlitMasterNode : AbstractLightweightMasterNode
    {
        public const string ColorSlotName = "Color";
        public const string AlphaSlotName = "Alpha";

        public const int ColorSlotId = 0;
        public const int AlphaSlotId = 1;

        public LightweightUnlitMasterNode()
        {
            name = "LightweightUnlitMasterNode";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(ColorSlotId, ColorSlotName, ColorSlotName, SlotType.Input, Vector3.zero, ShaderStage.Fragment));
            AddSlot(new Vector1MaterialSlot(AlphaSlotId, AlphaSlotName, AlphaSlotName, SlotType.Input, 0, ShaderStage.Fragment));

            // clear out slot names that do not match the slots
            // we support
            RemoveSlotsNameNotMatching(
                new[]
            {
                ColorSlotId,
                AlphaSlotId
            });
        }

        protected override IEnumerable<int> masterSurfaceInputs
        {
            get
            {
                return new[]
                {
                    ColorSlotId,
                    AlphaSlotId,
                };
            }
        }

        protected override IEnumerable<int> masterVertexInputs
        {
            get
            {
                return new int[]
                {
                };
            }
        }

        protected override string GetTemplateName()
        {
            return "lightweightSubshaderUnlit.template";
        }

        protected override int GetInterpolatorStartIndex()
        {
            return 0;
        }
    }
}
