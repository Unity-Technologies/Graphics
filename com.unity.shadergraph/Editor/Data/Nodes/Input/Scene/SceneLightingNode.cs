using System.Reflection;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using System.Linq;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Scene", "Scene Lighting")]
    class SceneLightingNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireCameraOpaqueTexture, IMayRequireScreenPosition
    {
        const int kUvInputSlotId = 0;
        const string kUvInputSlotName = "ScreenPosition";
       
        const int kLightingOutputSlotId = 1;
        const string kLightingOutputSlotName = "Lighting";

        public enum LightingType
        {
            SSAO, 
            SSR
        }

        public SceneLightingNode()
        {
            name = "Scene Lighting";
            UpdateNodeAfterDeserialization();
        }

        [SerializeField]
        LightingType m_LightingType = LightingType.SSAO;

        [EnumControl("Lighting")]
        public LightingType lightingType
        {
            get { return m_LightingType; }
            set
            {
                if (m_LightingType == value)
                    return;
              
                m_LightingType = value;
                Dirty(ModificationScope.Graph);
            }
        }

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new ScreenPositionMaterialSlot(kUvInputSlotId, kUvInputSlotName, kUvInputSlotName, ScreenSpaceType.Default, ShaderStageCapability.Fragment));
            AddSlot(new Vector4MaterialSlot(kLightingOutputSlotId, kLightingOutputSlotName, kLightingOutputSlotName, SlotType.Output, Vector2.zero, ShaderStageCapability.Fragment));
          
            RemoveSlotsNameNotMatching(new[] {kUvInputSlotId, kLightingOutputSlotId });
        }
        
        public virtual void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        { 
            string result = string.Empty;

            switch(m_LightingType)
            {
                case LightingType.SSAO:
                    result = string.Format("$precision4 {0} = SHADERGRAPH_LOAD_SCENE_SSAO({1}.xy);", GetVariableNameForSlot(kLightingOutputSlotId),
                        GetSlotValue(kUvInputSlotId, generationMode));
                    break;
                case LightingType.SSR:
                    result = string.Format("$precision4 {0} = SHADERGRAPH_LOAD_SCENE_SSR({1}.xy);", GetVariableNameForSlot(kLightingOutputSlotId),
                        GetSlotValue(kUvInputSlotId, generationMode));
                    break;                
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

