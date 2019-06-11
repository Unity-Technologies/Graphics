using System.Collections.Generic;
using System.Globalization;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    enum ConstantType
    {
        PI,
        TAU,
        PHI,
        E,
        SQRT2
    };

    [Title("Input", "Basic", "Constant")]
    class ConstantNode : AbstractMaterialNode, IGeneratesBodyCode
    {
        static Dictionary<ConstantType, float> m_constantList = new Dictionary<ConstantType, float>
        {
            {ConstantType.PI, 3.1415926f },
            {ConstantType.TAU, 6.28318530f},
            {ConstantType.PHI, 1.618034f},
            {ConstantType.E, 2.718282f},
            {ConstantType.SQRT2, 1.414214f},
        };

        [SerializeField]
        private ConstantType m_constant = ConstantType.PI;

        private const int kOutputSlotId = 0;
        private const string kOutputSlotName = "Out";

        [EnumControl("")]
        public ConstantType constant
        {
            get { return m_constant; }
            set
            {
                if (m_constant == value)
                    return;

                m_constant = value;
                Dirty(ModificationScope.Graph);
            }
        }

        public ConstantNode()
        {
            name = "Constant";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector1MaterialSlot(kOutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, 0));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotId });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GraphContext graphContext, GenerationMode generationMode)
        {
            sb.AppendLine(string.Format("$precision {0} = {1};"
                , GetVariableNameForNode()
                , m_constantList[constant].ToString(CultureInfo.InvariantCulture)));
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return GetVariableNameForNode();
        }
    }
}
