using UnityEngine;
using UnityEditor.Graphing;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.UIElements;


namespace UnityEditor.ShaderGraph
{
    class SampleTerrainHeightNodeBase : AbstractMaterialNode, IGeneratesBodyCode, IHasSettings
    {
        public const int WorldPosInputId = 0;
        public const int MipLevelInputId = 1;

        public const int WorldHeightOutputId = 2;
        public const int FeedbackSlotId = 3;
        public const int HeightAboveTerrainOutputId = 4;
        public const int WorldPositionOutputId = 5;

        const string WorldPosInputName = "Position";
        const string MipLevelInputName = "MipLevel";
        const string WorldHeightOutputName = "TerrainHeight";
        const string FeedbackSlotName = "Feedback";
        const string WorldPositionOutputName = "TerrainPosition";
        const string HeightAboveTerrainOutputName = "HeightAboveTerrain";

        [SerializeField]
        bool useFeedback = true;

        [SerializeField]
        string textureStackName = "_TerrainHeightmapStack";

        internal enum MipCalculation
        {
            Default,
            Explicit
        };

        [SerializeField]
        MipCalculation mipCalculation = MipCalculation.Default;

        public override bool hasPreview { get { return false; } }

        private bool explicitMip { get { return (mipCalculation == MipCalculation.Explicit); } }

        public SampleTerrainHeightNodeBase(MipCalculation mipCalculation)
        {
            this.mipCalculation = mipCalculation;
            if (explicitMip)
                name = "Sample Terrain Height LOD";
            else
                name = "Sample Terrain Height";
            UpdateNodeAfterDeserialization();
        }

        public override void UpdateNodeAfterDeserialization()
        {
            // Allocate IDs
            List<int> usedSlots = new List<int>();
            usedSlots.Add(WorldPosInputId);
            if (explicitMip)
                usedSlots.Add(MipLevelInputId);
            usedSlots.Add(WorldHeightOutputId);
            usedSlots.Add(HeightAboveTerrainOutputId);
            usedSlots.Add(WorldPositionOutputId);
            usedSlots.Add(FeedbackSlotId);

            // Create slots
            AddSlot(new PositionMaterialSlot(WorldPosInputId, WorldPosInputName, WorldPosInputName, CoordinateSpace.AbsoluteWorld));        // TODO: not absolute world!  use relative world
            if (explicitMip)
                AddSlot(new Vector1MaterialSlot(MipLevelInputId, MipLevelInputName, MipLevelInputName, SlotType.Input, 0.0f));

            AddSlot(new Vector1MaterialSlot(WorldHeightOutputId, WorldHeightOutputName, WorldHeightOutputName, SlotType.Output, 0.0f));
            AddSlot(new Vector1MaterialSlot(HeightAboveTerrainOutputId, HeightAboveTerrainOutputName, HeightAboveTerrainOutputName, SlotType.Output, 0.0f));
            AddSlot(new Vector3MaterialSlot(WorldPositionOutputId, WorldPositionOutputName, WorldPositionOutputName, SlotType.Output, Vector3.zero));

            // hidden feedback slot         TODO: do we let the user disable this slot, when we don't want to use terrain feedback?
            var slot = new Vector4MaterialSlot(FeedbackSlotId, FeedbackSlotName, FeedbackSlotName, SlotType.Output, Vector4.zero, ShaderStageCapability.Fragment);
            slot.hidden = true;
            AddSlot(slot);

            RemoveSlotsNameNotMatching(usedSlots);
        }

        public override void ValidateNode()
        {
            base.ValidateNode();
        }

        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview3D; }
        }

        public VisualElement CreateSettingsElement()
        {
            PropertySheet ps = new PropertySheet();

            var toggle = new Toggle("Use Virtual Texture Feedback");
            toggle.value = useFeedback;
            toggle.RegisterValueChangedCallback((t) => { useFeedback = t.newValue; });
            ps.Add(toggle);

            var strField = new UnityEngine.UIElements.TextField("Texture Stack Name");
            strField.value = textureStackName;
            strField.RegisterValueChangedCallback((t) => { textureStackName = t.newValue; });
            ps.Add(strField);

            return ps;
        }

        string GetTerrainHeightStackName()
        {
            return textureStackName;
        }

        string GetTerrainHeightLayerName()
        {
            return $"{textureStackName}_0";
        }

        public virtual void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            // Not all outputs may be connected (well one is or we wouldn't get called) so we are careful to
            // only generate code for connected outputs

            string stackName = GetTerrainHeightStackName();

            bool heightOutputConnected = IsSlotConnected(WorldHeightOutputId);
            bool heightAboveOutputConnected = IsSlotConnected(HeightAboveTerrainOutputId);
            bool positionOutputConnected = IsSlotConnected(WorldPositionOutputId);
            bool outputConnected = heightOutputConnected || heightAboveOutputConnected || positionOutputConnected;
            bool feedbackConnected = IsSlotConnected(FeedbackSlotId);

            if (outputConnected || feedbackConnected)
            {
                string result;
                if (explicitMip)
                {
                    result = string.Format("StackInfo {0}_info = PrepareStackLod(({1}).xz * {0}_WorldToUVTransform.xy + {0}_WorldToUVTransform.zw, {0}, {2});"
                            , stackName
                            , GetSlotValue(WorldPosInputId, generationMode)
                            , GetSlotValue(MipLevelInputId, generationMode));
                }
                else
                {
                    result = string.Format("StackInfo {0}_info = PrepareStack(({1}).xz * {0}_WorldToUVTransform.xy + {0}_WorldToUVTransform.zw, {0});"
                            , stackName
                            , GetSlotValue(WorldPosInputId, generationMode));
                }
                sb.AppendLine(result);
            }

            if (outputConnected)
            {
                var heightId = GetTerrainHeightLayerName();
                string resultHeight = string.Format("$precision {1} = {3}({0}_info, {2}).r * {0}_HeightTransform.x + {0}_HeightTransform.y;"
                        , stackName
                        , GetVariableNameForSlot(WorldHeightOutputId)
                        , heightId
                        , explicitMip ? "SampleStackLod" : "SampleStack");
                sb.AppendLine(resultHeight);
            }

            if (heightAboveOutputConnected)
            {
                string resultDelta = string.Format("$precision {0} = {1}.y - {2};"
                        , GetVariableNameForSlot(HeightAboveTerrainOutputId)
                        , GetSlotValue(WorldPosInputId, generationMode)
                        , GetVariableNameForSlot(WorldHeightOutputId));
                sb.AppendLine(resultDelta);
            }

            if (positionOutputConnected)
            {
                string resultPos = string.Format("$precision3 {0} = $precision3({2}.x, {1}, {2}.z);"
                        , GetVariableNameForSlot(WorldPositionOutputId)
                        , GetVariableNameForSlot(WorldHeightOutputId)
                        , GetSlotValue(WorldPosInputId, generationMode));
                sb.AppendLine(resultPos);
            }

            if (feedbackConnected && !explicitMip)      // TODO can we do feedback for explicit mip?  maybe, as long as it's pixel shader...
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
                slotNames = slotNames,
                m_Batchable = true
            });

            properties.AddShaderProperty(new StackShaderProperty()
            {
                overrideReferenceName = "float4 " + stackName + "_WorldToUVTransform",
                m_Batchable = true
            });

            properties.AddShaderProperty(new StackShaderProperty()
            {
                overrideReferenceName = "float4 " + stackName + "_HeightTransform",
                m_Batchable = true
            });

            properties.AddShaderProperty(new StackShaderProperty()
            {
                overrideReferenceName = stackName,
                slotNames = slotNames
            });
        }
    }

    [Title("Input", "Texture", "Sample Terrain Height")]
    class SampleTerrainHeightNode : SampleTerrainHeightNodeBase
    {
        public SampleTerrainHeightNode() : base(MipCalculation.Default)
        { }
    }


    [Title("Input", "Texture", "Sample Terrain Height LOD")]
    class SampleTerrainHeightLODNode : SampleTerrainHeightNodeBase
    {
        public SampleTerrainHeightLODNode() : base(MipCalculation.Explicit)
        { }
    }
}
