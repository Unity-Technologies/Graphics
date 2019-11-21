using System.Reflection;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using System.Linq;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Scene", "Scene Normal")]
    class SceneNormalNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireCameraOpaqueTexture, IMayRequireScreenPosition
    {
        const int kUvInputSlotId = 0;
        const string kUvInputSlotName = "ScreenPosition";
       
        const int kNormalOutputSlotId = 1;
        const string kNormalOutputSlotName = "Normal";
       
        public SceneNormalNode()
        {
            name = "Scene Normal";
            UpdateNodeAfterDeserialization();
        }
       
       
        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new ScreenPositionMaterialSlot(kUvInputSlotId, kUvInputSlotName, kUvInputSlotName, ScreenSpaceType.Default, ShaderStageCapability.Fragment));
            AddSlot(new Vector3MaterialSlot(kNormalOutputSlotId, kNormalOutputSlotName, kNormalOutputSlotName, SlotType.Output, Vector3.zero, ShaderStageCapability.Fragment));
          
            RemoveSlotsNameNotMatching(new[] {kUvInputSlotId, kNormalOutputSlotId });
        }
        
        public virtual void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        { 
            string result = string.Empty;

            result = string.Format("$precision3 {0} = SHADERGRAPH_LOAD_SCENE_NORMAL({1}.xy);", GetVariableNameForSlot(kNormalOutputSlotId),
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

