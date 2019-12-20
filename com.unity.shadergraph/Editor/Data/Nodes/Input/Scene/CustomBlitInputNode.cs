using System.Reflection;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Scene", "Custom Blit")]
    sealed class CustomBlitInputNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireCameraOpaqueTexture, IMayRequireScreenPosition
    {
        public enum InputTextureType
        {
            SceneColor,
            CustomColor,
            SceneDepth,
            CustomDepth,
            Normal,
        }

        public enum SamplingType
        {
            Sample,
            Load
        }

        const int kUvInputSlotId = 0;
        const string kUvInputSlotName = "UV";

        const int kColorOutputSlotId = 1;
        const string kColorOutputSlotName = "Output";

        [SerializeField]
        InputTextureType m_InputTextureType = InputTextureType.SceneColor;

        [SerializeField]
        SamplingType samplingType = SamplingType.Load;

        string GetCurrentInputTextureType()
        {
            return System.Enum.GetName(typeof(InputTextureType), m_InputTextureType);
        }

        public CustomBlitInputNode()
        {
            name = "Custom Blit";
            UpdateNodeAfterDeserialization();
        }

        [EnumControl("Input")]
        public InputTextureType inputTextureType
        {
            get { return m_InputTextureType; }
            set
            {
                if (m_InputTextureType == value)
                    return;

                m_InputTextureType = value;
                Dirty(ModificationScope.Graph);
            }
        }

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new ScreenPositionMaterialSlot(kUvInputSlotId, kUvInputSlotName, kUvInputSlotName, ScreenSpaceType.Default, ShaderStageCapability.Fragment));
            AddSlot(new Vector4MaterialSlot(kColorOutputSlotId, kColorOutputSlotName, kColorOutputSlotName, SlotType.Output, Vector4.zero, ShaderStageCapability.Fragment));

            RemoveSlotsNameNotMatching(new[] {kUvInputSlotId, kColorOutputSlotId});
        }
        
        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            string uv = GetSlotValue(kUvInputSlotId, generationMode);
            string result = string.Format("$precision4 {0} = SHADERGRAPH_LOAD_CUSTOM_BLIT_INPUT({1}.xy);", GetVariableNameForSlot(0), uv);
            sb.AppendLine(result);
        }
           
        public bool RequiresCameraOpaqueTexture(ShaderStageCapability stageCapability)
        {
            return true;
        }

        public bool RequiresScreenPosition(ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            return true;
        }
    }
}

