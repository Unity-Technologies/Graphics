using System.Reflection;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using System.Linq;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Scene", "Scene Roughness")]
    class SceneRoughnessNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireCameraOpaqueTexture, IMayRequireScreenPosition
    {
        const int kUvInputSlotId = 0;
        const string kUvInputSlotName = "ScreenPosition";
       
        const int kRoughnessOutputSlotId = 1;
        const string kRoughnessOutputSlotName = "Roughness";
       
        public SceneRoughnessNode()
        {
            name = "Scene Roughness";
            UpdateNodeAfterDeserialization();
        }
       
       
        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new ScreenPositionMaterialSlot(kUvInputSlotId, kUvInputSlotName, kUvInputSlotName, ScreenSpaceType.Default, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(kRoughnessOutputSlotId, kRoughnessOutputSlotName, kRoughnessOutputSlotName, SlotType.Output, 0, ShaderStageCapability.Fragment));
          
            RemoveSlotsNameNotMatching(new[] {kUvInputSlotId, kRoughnessOutputSlotId });
        }
        
        public virtual void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        { 
            string result = string.Empty;

            result = string.Format("$precision1 {0} = SHADERGRAPH_LOAD_SCENE_ROUGHNESS({1}.xy);", GetVariableNameForSlot(kRoughnessOutputSlotId),
                GetSlotValue(kUvInputSlotId, generationMode));
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

