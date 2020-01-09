using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using System.Linq;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Scene", "Scene Color")]
    class SceneColorNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireCameraOpaqueTexture, IMayRequireScreenPosition
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
        const int kLodInputSlotId = 3;
        const string kLodInputSlotName = "Lod";

        const int kColorOutputSlotId = 1;
        const string kColorOutputSlotName = "Output";

        [SerializeField]
        SamplingType m_SamplingType = SamplingType.Load;

        public SceneColorNode()
        {
            name = "Scene Color";
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
            AddSlot(new Vector3MaterialSlot(kColorOutputSlotId, kColorOutputSlotName, kColorOutputSlotName, SlotType.Output, Vector3.zero, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(kLodInputSlotId, kLodInputSlotName, kLodInputSlotName, SlotType.Input, 0, ShaderStageCapability.Fragment));
            if (m_SamplingType == SamplingType.Sample)
            {
                AddSlot(new SamplerStateMaterialSlot(kSamplerInputSlotId, kSamplerInputSlotName, kSamplerInputSlotName, SlotType.Input));
               
                RemoveSlotsNameNotMatching(new[] {kUvInputSlotId, kColorOutputSlotId, kSamplerInputSlotId, kLodInputSlotId });
            }
            else
            {
                RemoveSlotsNameNotMatching(new[] {kUvInputSlotId, kColorOutputSlotId, kLodInputSlotId });
            }
        }
        
        public virtual void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        { 
            string result = string.Empty;
            if (m_SamplingType == SamplingType.Load)
            {
                result = string.Format("$precision3 {0} = SHADERGRAPH_LOAD_SCENE_COLOR({1}.xy, {2});", GetVariableNameForSlot(kColorOutputSlotId),
                    GetSlotValue(kUvInputSlotId, generationMode),
                    GetSlotValue(kLodInputSlotId, generationMode));
            }
            else
            {
                result = string.Format("$precision3 {0} = SHADERGRAPH_SAMPLE_SCENE_COLOR({1}.xy, {2}, {3});", GetVariableNameForSlot(kColorOutputSlotId),
                    GetSlotValue(kUvInputSlotId, generationMode),
                    GetSlotValue(kSamplerInputSlotId, generationMode),
                    GetSlotValue(kLodInputSlotId , generationMode));
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
