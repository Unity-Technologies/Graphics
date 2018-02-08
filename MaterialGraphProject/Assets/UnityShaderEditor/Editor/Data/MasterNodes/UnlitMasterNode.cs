using System;
using System.Linq;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    [Title("Master", "Unlit")]
    public class UnlitMasterNode : MasterNode<IUnlitSubShader>
    {
        public const string ColorSlotName = "Color";
        public const string AlphaSlotName = "Alpha";
        public const string AlphaClipThresholdSlotName = "AlphaClipThreshold";
        public const string VertexOffsetName = "VertexPosition";

        public const int ColorSlotId = 0;
        public const int AlphaSlotId = 7;
        public const int AlphaThresholdSlotId = 8;

       
        [SerializeField]
        private AlphaMode m_AlphaMode;

        [EnumControl("")]
        public AlphaMode alphaMode
        {
            get { return m_AlphaMode; }
            set
            {
                if (m_AlphaMode == value)
                    return;

                m_AlphaMode = value;
                Dirty(ModificationScope.Graph);
            }
        }

        public UnlitMasterNode()
        {
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            name = "Unlit Master";
            AddSlot(new ColorRGBMaterialSlot(ColorSlotId, ColorSlotName, ColorSlotName, SlotType.Input, Color.grey, ShaderStage.Fragment));
            AddSlot(new Vector1MaterialSlot(AlphaSlotId, AlphaSlotName, AlphaSlotName, SlotType.Input, 1, ShaderStage.Fragment));
            AddSlot(new Vector1MaterialSlot(AlphaThresholdSlotId, AlphaClipThresholdSlotName, AlphaClipThresholdSlotName, SlotType.Input, 0f, ShaderStage.Fragment));

            // clear out slot names that do not match the slots
            // we support
            RemoveSlotsNameNotMatching(
                new[]
            {
                ColorSlotId,
                AlphaSlotId,
                AlphaThresholdSlotId
            });

            if (!subShaders.Any())
                AddSubShader(new LightWeightUnlitSubShader());
        }
    }
}
