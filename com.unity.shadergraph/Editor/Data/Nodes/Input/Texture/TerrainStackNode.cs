using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using System.Collections.Generic;
using System;
using System.Globalization;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Texture", "Sample Terrain Height")]
    class SampleTerrainHeightNode : AbstractMaterialNode, IGeneratesBodyCode        //, IMayRequireMeshUV
    {
        public const int WorldPosInputId = 0;
        public const int MipLevelInputId = 1;
        public const int WorldHeightOutputId = 2;
        public const int FeedbackSlotId = 3;

        const string WorldPosInputName = "WorldPos";
        const string MipLevelInputName = "MipLevel";
        const string WorldHeightOutputName = "WorldHeight";
        const string FeedbackSlotName = "Feedback";

        int[] liveIds;

        public override bool hasPreview { get { return false; } }

        public SampleTerrainHeightNode()
        {
            name = "Sample Terrain Height";
            UpdateNodeAfterDeserialization();
        }
        public override void UpdateNodeAfterDeserialization()
        {
            // Allocate IDs
            List<int> usedSlots = new List<int>();
            usedSlots.Add(WorldPosInputId);
            usedSlots.Add(MipLevelInputId);
            usedSlots.Add(WorldHeightOutputId);
            usedSlots.Add(FeedbackSlotId);

            liveIds = usedSlots.ToArray();

            // Create slots
            AddSlot(new PositionMaterialSlot(WorldPosInputId, WorldPosInputName, WorldPosInputName, CoordinateSpace.AbsoluteWorld));        // TODO: not absolute world!  use relative world
            AddSlot(new Vector1MaterialSlot(MipLevelInputId, MipLevelInputName, MipLevelInputName, SlotType.Input, 0.0f));
            AddSlot(new Vector1MaterialSlot(WorldHeightOutputId, WorldHeightOutputName, WorldHeightOutputName, SlotType.Output, 0.0f));

            // hidden feedback slot         TODO: do we let the user disable this slot, when we don't want to use terrain feedback?
            var slot = new Vector4MaterialSlot(FeedbackSlotId, FeedbackSlotName, FeedbackSlotName, SlotType.Output, Vector4.zero, ShaderStageCapability.Fragment);
            slot.hidden = true;
            AddSlot(slot);

            RemoveSlotsNameNotMatching(liveIds);
        }

        public override void ValidateNode()
        {
            base.ValidateNode();
        }

        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview3D; }
        }

        string GetTerrainHeightStackName()
        {
            return "HeightmapStack";        // TODO: this should be set based on the name of the Terrain system -- so we can have multiple terrains
        }

        string GetTerrainHeightLayerName()
        {
            return "Height";
        }

        public virtual void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            // Not all outputs may be connected (well one is or we wouldn't get called) so we are careful to
            // only generate code for connected outputs

            string stackName = GetTerrainHeightStackName();

            bool outputConnected = IsSlotConnected(WorldHeightOutputId);
            bool feedbackConnected = IsSlotConnected(FeedbackSlotId); ;

            if (outputConnected || feedbackConnected)
            {
                string result = string.Format("StackInfo {0}_info = PrepareStack(({1}).xz * {0}_worldToUVTransform.xy + {0}_worldToUVTransform.zw, {0});"
                        , stackName
                        , GetSlotValue(WorldPosInputId, generationMode));
                sb.AppendLine(result);
            }

            if (outputConnected)
            {
                var heightId = GetTerrainHeightLayerName();
                string resultLayer = string.Format("$precision4 {1} = SampleStack({0}_info, {2});"
                        , stackName
                        , GetVariableNameForSlot(WorldHeightOutputId)
                        , heightId);
                sb.AppendLine(resultLayer);
            }

            if (feedbackConnected)
            {
                //TODO: Investigate if the feedback pass can use halfs
                string feedBackCode = string.Format("float4 {0} = GetResolveOutput({1}_info);",
                        GetVariableNameForSlot(FeedbackSlotId),
                        stackName);
                sb.AppendLine(feedBackCode);
            }
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            base.CollectShaderProperties(properties, generationMode);

            // Get names of connected textures
            List<string> slotNames = new List<string>();
            slotNames.Add(GetTerrainHeightLayerName());

            string stackName = GetTerrainHeightStackName();

            properties.AddShaderProperty(new StackShaderProperty()
            {
                overrideReferenceName = stackName + "_cb",
                m_Batchable = true,
                slotNames = slotNames
            });

            properties.AddShaderProperty(new StackShaderProperty()
            {
                overrideReferenceName = "float4 " + stackName + "_worldToUVTransform",
                m_Batchable = true
            });

            properties.AddShaderProperty(new StackShaderProperty()
            {
                overrideReferenceName = stackName,
                slotNames = slotNames
            });
        }


    }

}
