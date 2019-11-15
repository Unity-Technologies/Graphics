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
    [Title("Input", "Texture", "Sample Terrain Material")]
    class SampleTerrainMaterialNode : AbstractMaterialNode, IGeneratesBodyCode        //, IMayRequireMeshUV
    {
        public const int WorldPosInputId = 0;
//        public const int MipLevelInputId = 1;         // TODO: add mip bias as optional input ?
        public const int AlbedoOutputId = 2;
        public const int NormalOutputId = 3;
        public const int SmoothnessOutputId = 4;
        public const int AmbientOcclusionOutputId = 5;
        public const int MetallicOutputId = 6;
        public const int FeedbackSlotId = 7;

        const string WorldPosInputName = "Position";
//        const string MipLevelInputName = "MipLevel";
        const string AlbedoOutputName = "Color";
        const string NormalOutputName = "Normal";
        const string SmoothnessOutputName = "Smoothness";
        const string AmbientOcclusionOutputName = "AmbientOcclusion";
        const string MetallicOutputName = "Metallic";
        const string FeedbackSlotName = "Feedback";

        int[] liveIds;

        public override bool hasPreview { get { return false; } }

        public SampleTerrainMaterialNode()
        {
            name = "Sample Terrain Height";
            UpdateNodeAfterDeserialization();
        }
        public override void UpdateNodeAfterDeserialization()
        {
            // Allocate IDs
            List<int> usedSlots = new List<int>();
            usedSlots.Add(WorldPosInputId);
//            usedSlots.Add(MipLevelInputId);
            usedSlots.Add(AlbedoOutputId);
            usedSlots.Add(NormalOutputId);
            usedSlots.Add(SmoothnessOutputId);
            usedSlots.Add(AmbientOcclusionOutputId);
            usedSlots.Add(MetallicOutputId);
            usedSlots.Add(FeedbackSlotId);

            liveIds = usedSlots.ToArray();

            // Create slots
            AddSlot(new PositionMaterialSlot(WorldPosInputId, WorldPosInputName, WorldPosInputName, CoordinateSpace.AbsoluteWorld));        // TODO: not absolute world!  use relative world
                                                                                                                                            //            AddSlot(new Vector1MaterialSlot(MipLevelInputId, MipLevelInputName, MipLevelInputName, SlotType.Input, 0.0f));

            AddSlot(new Vector3MaterialSlot(AlbedoOutputId, AlbedoOutputName, AlbedoOutputName, SlotType.Output, Vector3.one));
            AddSlot(new Vector3MaterialSlot(NormalOutputId, NormalOutputName, NormalOutputName, SlotType.Output, new Vector3(0.0f, 1.0f, 0.0f)));
            AddSlot(new Vector1MaterialSlot(SmoothnessOutputId, SmoothnessOutputName, SmoothnessOutputName, SlotType.Output, 0.0f));
            AddSlot(new Vector1MaterialSlot(AmbientOcclusionOutputId, AmbientOcclusionOutputName, AmbientOcclusionOutputName, SlotType.Output, 1.0f));
            AddSlot(new Vector1MaterialSlot(MetallicOutputId, MetallicOutputName, MetallicOutputName, SlotType.Output, 0.0f));

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

        string GetTerrainMaterialStackName()
        {
            return "MaterialStack";        // TODO: this should be set based on the name of the Terrain system -- so we can have multiple terrains
        }

        string GetTerrainAlbedoLayerName()
        {
            return "Albedo";
        }

        string GetTerrainNormalLayerName()
        {
            return "Normal";
        }

        string GetTerrainSpecularLayerName()
        {
            return "Specular";
        }

        public virtual void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            // Not all outputs may be connected (well one is or we wouldn't get called) so we are careful to
            // only generate code for connected outputs

            string stackName = GetTerrainMaterialStackName();

            bool specularConnected =
                IsSlotConnected(SmoothnessOutputId) ||
                IsSlotConnected(AmbientOcclusionOutputId) ||
                IsSlotConnected(MetallicOutputId);

            bool outputConnected =
                IsSlotConnected(AlbedoOutputId) ||
                IsSlotConnected(NormalOutputId) ||
                specularConnected;

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
                // TODO : the decoding here needs to match what the pixel cache
                if (IsSlotConnected(AlbedoOutputId))
                {
                    string albedo = string.Format("$precision3 {0} = SampleStack({1}_info, {2});"
                            , GetVariableNameForSlot(AlbedoOutputId)
                            , stackName
                            , GetTerrainAlbedoLayerName());
                    sb.AppendLine(albedo);
                }

                if (IsSlotConnected(NormalOutputId))
                {
                    string normal = string.Format("$precision3 {0} = SampleStack({1}_info, {2});"
                            , GetVariableNameForSlot(NormalOutputId)
                            , stackName
                            , GetTerrainNormalLayerName());
                    sb.AppendLine(normal);
                }

                if (specularConnected)
                {
                    string specular = string.Format("$precision4 {0} = SampleStack({1}_info, {2});"
                            , "specularSample"
                            , stackName
                            , GetTerrainSpecularLayerName());
                    sb.AppendLine(specular);

                    if (IsSlotConnected(SmoothnessOutputId))
                    {
                        string smoothness = string.Format("$precision {0} = specularSample.x;"
                                , GetVariableNameForSlot(SmoothnessOutputId));
                        sb.AppendLine(smoothness);
                    }

                    if (IsSlotConnected(AmbientOcclusionOutputId))
                    {
                        string smoothness = string.Format("$precision {0} = specularSample.y;"
                                , GetVariableNameForSlot(AmbientOcclusionOutputId));
                        sb.AppendLine(smoothness);
                    }

                    if (IsSlotConnected(MetallicOutputId))
                    {
                        string smoothness = string.Format("$precision {0} = specularSample.z;"
                                , GetVariableNameForSlot(MetallicOutputId));
                        sb.AppendLine(smoothness);
                    }
                }
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
            slotNames.Add(GetTerrainAlbedoLayerName());
            slotNames.Add(GetTerrainNormalLayerName());
            slotNames.Add(GetTerrainSpecularLayerName());

            string stackName = GetTerrainMaterialStackName();

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
