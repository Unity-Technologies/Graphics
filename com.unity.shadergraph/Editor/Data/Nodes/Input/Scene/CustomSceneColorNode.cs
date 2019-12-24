using System.Reflection;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using System.Linq;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Scene", "Custom Scene Color")]
    class CustomSceneColorNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireCameraOpaqueTexture, IMayRequireScreenPosition
    {
        public enum SamplingType
        {
            Sample,
            Load
        }

        const int kUvInputSlotId = 0;
        const string kUvInputSlotName = "ScreenPosition";
        const int kSamplerInputSlotId = 2;
        const string kSamplerInputSlotName = "Sampler";

        const int kColorOutputSlotId = 1;
        const string kColorOutputSlotName = "Output";

        [SerializeField]
        SamplingType m_SamplingType = SamplingType.Load;

        public CustomSceneColorNode()
        {
            name = "Custom Scene Color";
            UpdateNodeAfterDeserialization();
        }
       
        [EnumControl("Sampling")]
        public SamplingType samplingType
        {
            get { return m_SamplingType; }
            set
            {
                if (m_SamplingType == value)
                    return;
                if(m_SamplingType == SamplingType.Sample)
                {
                    RemoveSlot(kSamplerInputSlotId);
                }
                else
                {
                    AddSlot(new SamplerStateMaterialSlot(kSamplerInputSlotId, kSamplerInputSlotName, kSamplerInputSlotName, SlotType.Input));
                }
                m_SamplingType = value;
                Dirty(ModificationScope.Graph);
            }
        }

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new ScreenPositionMaterialSlot(kUvInputSlotId, kUvInputSlotName, kUvInputSlotName, ScreenSpaceType.Default, ShaderStageCapability.Fragment));
            AddSlot(new Vector4MaterialSlot(kColorOutputSlotId, kColorOutputSlotName, kColorOutputSlotName, SlotType.Output, Vector4.zero, ShaderStageCapability.Fragment));
            if (m_SamplingType == SamplingType.Sample)
            {
                AddSlot(new SamplerStateMaterialSlot(kSamplerInputSlotId, kSamplerInputSlotName, kSamplerInputSlotName, SlotType.Input));
            }
            RemoveSlotsNameNotMatching(new[] {kUvInputSlotId, kColorOutputSlotId, kSamplerInputSlotId });
        }
        
        public virtual void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        { 
            string result = string.Empty;
            if (m_SamplingType == SamplingType.Load)
            {
                result = string.Format("$precision4 {0} = SHADERGRAPH_LOAD_CUSTOM_SCENE_COLOR({1}.xy);", GetVariableNameForSlot(kColorOutputSlotId),
                    GetSlotValue(kUvInputSlotId, generationMode));
            }
            else
            {
                result = string.Format("$precision4 {0} = SHADERGRAPH_SAMPLE_CUSTOM_SCENE_COLOR({1}.xy, {2});", GetVariableNameForSlot(kColorOutputSlotId),
                    GetSlotValue(kUvInputSlotId, generationMode),
                    GetSlotValue(kSamplerInputSlotId, generationMode));
            }
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

