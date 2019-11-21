using System.Reflection;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using System.Linq;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Scene", "Scene Motion Vector")]
    class SceneMotionVectorNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireCameraOpaqueTexture, IMayRequireScreenPosition
    {
        const int kUvInputSlotId = 0;
        const string kUvInputSlotName = "ScreenPosition";
       
        const int kMotionVectorOutputSlotId = 1;
        const string kMotionVectorOutputSlotName = "Motion Vector";
       
        public SceneMotionVectorNode()
        {
            name = "Scene Motion Vector";
            UpdateNodeAfterDeserialization();
        }
       
       
        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new ScreenPositionMaterialSlot(kUvInputSlotId, kUvInputSlotName, kUvInputSlotName, ScreenSpaceType.Default, ShaderStageCapability.Fragment));
            AddSlot(new Vector2MaterialSlot(kMotionVectorOutputSlotId, kMotionVectorOutputSlotName, kMotionVectorOutputSlotName, SlotType.Output, Vector2.zero, ShaderStageCapability.Fragment));
          
            RemoveSlotsNameNotMatching(new[] { kUvInputSlotId, kMotionVectorOutputSlotId });
        }
        
        public virtual void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        { 
            string result = string.Empty;

            result = string.Format("$precision2 {0} = SHADERGRAPH_LOAD_SCENE_MOTIONVECTOR({1}.xy);", GetVariableNameForSlot(kMotionVectorOutputSlotId),
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

