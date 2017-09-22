using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text.RegularExpressions;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    [Title("Master/Lightweight/Unlit")]
    public class LightweightUnlitMasterNode : AbstractLightweightMasterNode
    {
        public const string ColorSlotName = "Color";
        public const string AlphaSlotName = "Alpha";
        public const string VertexOffsetName = "VertexPosition";

        public const int ColorSlotId = 0;
        public const int AlphaSlotId = 1;
        public const int VertexOffsetId = 2;

        public LightweightUnlitMasterNode()
        {
            name = "LightweightUnlitMasterNode";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new MaterialSlot(VertexOffsetId, VertexOffsetName, VertexOffsetName, SlotType.Input, SlotValueType.Vector3, Vector4.zero, ShaderStage.Vertex));
            AddSlot(new MaterialSlot(ColorSlotId, ColorSlotName, ColorSlotName, SlotType.Input, SlotValueType.Vector3, Vector4.zero, ShaderStage.Fragment));
            AddSlot(new MaterialSlot(AlphaSlotId, AlphaSlotName, AlphaSlotName, SlotType.Input, SlotValueType.Vector1, Vector4.zero, ShaderStage.Fragment));

            // clear out slot names that do not match the slots
            // we support
            RemoveSlotsNameNotMatching(
                new[]
            {
                ColorSlotId,
                AlphaSlotId,
                VertexOffsetId
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
                return new[]
                {
                    VertexOffsetId
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
