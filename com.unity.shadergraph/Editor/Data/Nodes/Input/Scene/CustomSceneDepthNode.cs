using System.Reflection;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using System.Linq;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Scene", "Custom Scene Depth")]
    class CustomSceneDepthNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireCameraOpaqueTexture, IMayRequireScreenPosition
    {
        public enum DepthSamplingMode
        {
            Linear01,
            Raw,
            Eye
        };

        const int kUvInputSlotId = 0;
        const string kUvInputSlotName = "ScreenPosition";
      
        const int kDepthOutputSlotId = 1;
        const string kDepthOutputSlotName = "Depth";

        [SerializeField]
        DepthSamplingMode m_SamplingMode = DepthSamplingMode.Linear01;

        public CustomSceneDepthNode()
        {
            name = "Custom Scene Depth";
            UpdateNodeAfterDeserialization();
        }
       
        [EnumControl("Sample Mode")]
        public DepthSamplingMode samplingMode
        {
            get { return m_SamplingMode; }
            set
            {
                if (m_SamplingMode == value)
                    return;
             
                m_SamplingMode = value;
                Dirty(ModificationScope.Graph);
            }
        }

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new ScreenPositionMaterialSlot(kUvInputSlotId, kUvInputSlotName, kUvInputSlotName, ScreenSpaceType.Default, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(kDepthOutputSlotId, kDepthOutputSlotName, kDepthOutputSlotName, SlotType.Output, 0, ShaderStageCapability.Fragment));
          
            RemoveSlotsNameNotMatching(new[] {kUvInputSlotId, kDepthOutputSlotId });
        }
        
        public virtual void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        { 
            string result = string.Empty;
            switch (m_SamplingMode)
            {
                case DepthSamplingMode.Linear01:
                {
                    result = string.Format("$precision1 {0} = Linear01Depth(SHADERGRAPH_LOAD_CUSTOM_SCENE_DEPTH({1}.xy), _ZBufferParams);", GetVariableNameForSlot(kDepthOutputSlotId),
                        GetSlotValue(kUvInputSlotId, generationMode));
                    break;
                }

                case DepthSamplingMode.Raw:
                {
                    result = string.Format("$precision1 {0} = SHADERGRAPH_LOAD_CUSTOM_SCENE_DEPTH({1}.xy);", GetVariableNameForSlot(kDepthOutputSlotId),
                        GetSlotValue(kUvInputSlotId, generationMode));
                    break;
                }

                case DepthSamplingMode.Eye:
                {
                    result = string.Format("$precision1 {0} = LinearEyeDepth(SHADERGRAPH_LOAD_CUSTOM_SCENE_DEPTH({1}.xy), _ZBufferParams);", GetVariableNameForSlot(kDepthOutputSlotId),
                        GetSlotValue(kUvInputSlotId, generationMode));
                    break;
                }
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

