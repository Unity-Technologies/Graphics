using UnityEngine;
using UnityEditor.Graphing;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.UIElements;


namespace UnityEditor.ShaderGraph
{
    class SampleTerrainMaterialNodeBase : AbstractMaterialNode, IGeneratesBodyCode, IHasSettings
    {
        public const int WorldPosInputId = 0;
        public const int MipLevelInputId = 1;         // TODO: add mip bias as optional input ?
        public const int AlbedoOutputId = 2;
        public const int NormalOutputId = 3;
        public const int SmoothnessOutputId = 4;
        public const int AmbientOcclusionOutputId = 5;
        public const int MetallicOutputId = 6;
        public const int FeedbackSlotId = 7;

        const string WorldPosInputName = "Position";
        const string MipLevelInputName = "MipLevel";
        const string AlbedoOutputName = "Color";
        const string NormalOutputName = "Normal";
        const string SmoothnessOutputName = "Smoothness";
        const string AmbientOcclusionOutputName = "AmbientOcclusion";
        const string MetallicOutputName = "Metallic";
        const string FeedbackSlotName = "Feedback";

        int[] liveIds;

        [SerializeField]
        bool useFeedback = true;

        [SerializeField]
        string textureStackName = "_TerrainTextureStack";

        internal enum MipCalculation
        {
            Default,
            Explicit
        };

        [SerializeField]
        MipCalculation mipCalculation = MipCalculation.Default;

        public override bool hasPreview { get { return false; } }
        private bool explicitMip { get { return (mipCalculation == MipCalculation.Explicit); } }

        public SampleTerrainMaterialNodeBase(MipCalculation mipCalculation)
        {
            this.mipCalculation = mipCalculation;
            if (explicitMip)
                name = "Sample Terrain Material LOD";
            else
                name = "Sample Terrain Material";
            UpdateNodeAfterDeserialization();
        }
        public override void UpdateNodeAfterDeserialization()
        {
            // Allocate IDs
            List<int> usedSlots = new List<int>();
            usedSlots.Add(WorldPosInputId);
            if (explicitMip)
                usedSlots.Add(MipLevelInputId);
            usedSlots.Add(AlbedoOutputId);
            usedSlots.Add(NormalOutputId);
            usedSlots.Add(SmoothnessOutputId);
            usedSlots.Add(AmbientOcclusionOutputId);
            usedSlots.Add(MetallicOutputId);
            usedSlots.Add(FeedbackSlotId);

            liveIds = usedSlots.ToArray();

            // Create slots
            AddSlot(new PositionMaterialSlot(WorldPosInputId, WorldPosInputName, WorldPosInputName, CoordinateSpace.AbsoluteWorld));        // TODO: not absolute world!  use relative world
            if (explicitMip)
                AddSlot(new Vector1MaterialSlot(MipLevelInputId, MipLevelInputName, MipLevelInputName, SlotType.Input, 0.0f));

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

        string GetTerrainMaterialStackName()
        {
            return textureStackName;
        }

        string GetTerrainAlbedoLayerName()
        {
            return "AlbedoSlot";
        }

        string GetTerrainNormalLayerName()
        {
            return "NormalSlot";
        }

        string GetTerrainSpecularLayerName()
        {
            return "MaskSlot";
        }

        public virtual void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            // Not all outputs may be connected (well one is or we wouldn't get called) so we are careful to
            // only generate code for connected outputs

            string stackName = GetTerrainMaterialStackName();

            bool albedoConnected = IsSlotConnected(AlbedoOutputId);
            bool normalConnected = IsSlotConnected(NormalOutputId);
            bool smoothnessConnected = IsSlotConnected(SmoothnessOutputId);
            bool ambientOcclusionConnected = IsSlotConnected(AmbientOcclusionOutputId);
            bool metallicConnected = IsSlotConnected(MetallicOutputId);

            bool anyOutputConnected = smoothnessConnected || ambientOcclusionConnected || metallicConnected || albedoConnected || normalConnected;

            bool feedbackConnected = IsSlotConnected(FeedbackSlotId); ;

            if (anyOutputConnected || feedbackConnected)
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

            if (anyOutputConnected)
            {
                if (albedoConnected || smoothnessConnected)
                {
                    string albedoSlot = string.Format("$precision4 {0} = {3}({1}_info, {2});"
                            , "albedoSlotSample"
                            , stackName
                            , GetTerrainAlbedoLayerName()
                            , explicitMip ? "SampleStack_Lod" : "SampleStack");
                    sb.AppendLine(albedoSlot);

                    if (albedoConnected)
                    {
                        string albedo = string.Format("$precision3 {0} = albedoSlotSample.rgb;"
                                , GetVariableNameForSlot(AlbedoOutputId));
                        sb.AppendLine(albedo);
                    }

                    if (smoothnessConnected)
                    {
                        string smoothness = string.Format("$precision {0} = albedoSlotSample.a;"
                                , GetVariableNameForSlot(SmoothnessOutputId));
                        sb.AppendLine(smoothness);
                    }
                }

                if (normalConnected)
                {
                    string normal = string.Format("$precision3 {0} = {3}({1}_info, {2}).rgb * 2.0f - 1.0f;"
                            , GetVariableNameForSlot(NormalOutputId)
                            , stackName
                            , GetTerrainNormalLayerName()
                            , explicitMip ? "SampleStackLod" : "SampleStack");
                    sb.AppendLine(normal);
                }

                if (ambientOcclusionConnected || metallicConnected)
                {
                    string specular = string.Format("$precision4 {0} = {3}({1}_info, {2});"
                            , "maskSlotSample"
                            , stackName
                            , GetTerrainSpecularLayerName()
                            , explicitMip ? "SampleStack_Lod" : "SampleStack");
                    sb.AppendLine(specular);

                    if (ambientOcclusionConnected)
                    {
                        string ao = string.Format("$precision {0} = maskSlotSample.g;"
                                , GetVariableNameForSlot(AmbientOcclusionOutputId));
                        sb.AppendLine(ao);
                    }

                    if (metallicConnected)
                    {
                        string metallic = string.Format("$precision {0} = maskSlotSample.r;"
                                , GetVariableNameForSlot(MetallicOutputId));
                        sb.AppendLine(metallic);
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
                overrideReferenceName = "float4 " + stackName + "_WorldToUVTransform",
                m_Batchable = true
            });

            properties.AddShaderProperty(new StackShaderProperty()
            {
                overrideReferenceName = stackName,
                slotNames = slotNames
            });
        }
    }

    [Title("Input", "Texture", "Sample Terrain Material")]
    class SampleTerrainMaterialNode : SampleTerrainMaterialNodeBase
    {
        public SampleTerrainMaterialNode() : base(MipCalculation.Default)
        { }
    }


    [Title("Input", "Texture", "Sample Terrain Material LOD")]
    class SampleTerrainMaterialLODNode : SampleTerrainMaterialNodeBase
    {
        public SampleTerrainMaterialLODNode() : base(MipCalculation.Explicit)
        { }
    }

}
