using System.Collections;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Terrain", "Terrain Texture")]
    // should do this so the node is only available in the correct subtargets
    // unclear how to accomplish this with a cross pipeline node, but pipeline specific targets
    [SubTargetFilterAttribute(new[] { typeof(ITerrainSubTarget)})]
    class TerrainTexture : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction
    {
        public TerrainTexture()
        {
            name = "Terrain Texture";
            UpdateNodeAfterDeserialization();
        }

        const int InputIndexId = 0;
        const int OutputTextureId = 1;
        const int OutputAvailableId = 2;
        const int NormalScaleId = 3;
        const int MetallicDefaultId = 4;
        const int SmoothnessDefaultId = 5;
        const int DiffuseRemapId = 6;
        const int OpacityAsDensityId = 7;
        const int MaskRemapOffsetId = 8;
        const int MaskRemapScaleId = 9;

        const string kInputIndexSlotName = "Index";
        const string kOutputTextureSlotName = "Texture";
        const string kOutputAvailableSlotName = "Available";
        const string kOutputNormalScaleSlotName = "Normal Scale";
        const string kOutputMetallicDefaultSlotName = "Metallic Default";
        const string kOutputSmoothnessDefaultSlotName = "Smoothness Default";
        const string kOutputDiffuseRemapSlotName = "Color Tint";
        // in the terrainLitInput this is called "_DiffuseRemapScaleX" (0-3) and stored only in the W component
        const string kOutputOpacityAsDensitySlotName = "Opacity As Density";
        const string kOutputMaskRemapOffsetSlotName = "Channel Remapping Offset";
        const string kOutputMaskRemapScaleSlotName = "Channel Remapping Scale";

        internal enum TextureType
        {
            DiffuseMap = 0,
            NormalMap = 1,
            MaskMap = 2,
            LayerMasks = 3,
            Holes
        }

        [SerializeField]
        private TextureType m_TextureType;

        [EnumControl("")]
        public TextureType textureType
        {
            get { return m_TextureType; }
            set { m_TextureType = value; Dirty(ModificationScope.Graph); }
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector1MaterialSlot(InputIndexId, kInputIndexSlotName, kInputIndexSlotName, SlotType.Input, 0, literal:true));
            AddSlot(new Texture2DMaterialSlot(OutputTextureId, kOutputTextureSlotName, kOutputTextureSlotName, SlotType.Output));
            AddSlot(new BooleanMaterialSlot(OutputAvailableId, kOutputAvailableSlotName, kOutputAvailableSlotName, SlotType.Output, false));
            AddSlot(new Vector1MaterialSlot(NormalScaleId, kOutputNormalScaleSlotName, kOutputNormalScaleSlotName, SlotType.Output, 1));
            AddSlot(new Vector1MaterialSlot(MetallicDefaultId, kOutputMetallicDefaultSlotName, kOutputMetallicDefaultSlotName, SlotType.Output, 0.0f));
            AddSlot(new Vector1MaterialSlot(SmoothnessDefaultId, kOutputSmoothnessDefaultSlotName, kOutputSmoothnessDefaultSlotName, SlotType.Output, 0.5f));
            AddSlot(new Vector3MaterialSlot(DiffuseRemapId, kOutputDiffuseRemapSlotName, kOutputDiffuseRemapSlotName, SlotType.Output, new Vector3(1.0f, 1.0f, 1.0f)));
            AddSlot(new Vector1MaterialSlot(OpacityAsDensityId, kOutputOpacityAsDensitySlotName, kOutputOpacityAsDensitySlotName, SlotType.Output, 1.0f));
            AddSlot(new Vector4MaterialSlot(MaskRemapOffsetId, kOutputMaskRemapOffsetSlotName, kOutputMaskRemapOffsetSlotName, SlotType.Output, new Vector4(0.0f, 0.0f, 0.0f, 0.0f)));
            AddSlot(new Vector4MaterialSlot(MaskRemapScaleId, kOutputMaskRemapScaleSlotName, kOutputMaskRemapScaleSlotName, SlotType.Output, new Vector4(1.0f, 1.0f, 1.0f, 1.0f)));
            RemoveSlotsNameNotMatching(new[] { InputIndexId, OutputTextureId, OutputAvailableId, NormalScaleId, MetallicDefaultId, SmoothnessDefaultId, DiffuseRemapId, OpacityAsDensityId, MaskRemapOffsetId, MaskRemapScaleId });
        }

        private bool Any(IEnumerable collection)
        {
            foreach (var x in collection)
                return true;
            return false;
        }

        public void GenerateNodeCode(ShaderStringBuilder s, GenerationMode generationMode)
        {
            var availableEdge = owner.GetEdges(FindOutputSlot<MaterialSlot>(OutputAvailableId).slotReference);
            var textureEdge = owner.GetEdges(FindOutputSlot<MaterialSlot>(OutputTextureId).slotReference);

            var indexValue = GetSlotValue(InputIndexId, generationMode);

            GenerateAdditionalParameterOutputs(s, generationMode, indexValue);

            var outputTextureSlotType = FindOutputSlot<Texture2DMaterialSlot>(OutputTextureId).concreteValueType.ToShaderString();
            if (generationMode.IsPreview())
            {
                if (Any(availableEdge)) s.AppendLine("$precision {0} = 1;", GetVariableNameForSlot(OutputAvailableId));
                if (Any(textureEdge)) s.AppendLine("{0} {1} = UnityBuildTexture2DStructNoScale(_TerrainPreviewTexture);", outputTextureSlotType, GetVariableNameForSlot(OutputTextureId));
                return;
            }

            void TextureAvailable(string textureName)
            {
                s.AppendLine("#if defined(SHADERGRAPH_PREVIEW) || defined(SHADERGRAPH_PREVIEW_MAIN)");
                s.AppendLine("$precision {0} = 1.0f;", GetVariableNameForSlot(OutputAvailableId));
                s.AppendLine("#else");
                s.AppendLine("$precision {0} = Unity_TerrainTextureAvailable({1}, {2});", GetVariableNameForSlot(OutputAvailableId), textureName, indexValue);
                s.AppendLine("#endif");
            }

            void TextureOutput(string textureName)
            {
                s.AppendLine("#if defined(SHADERGRAPH_PREVIEW) || defined(SHADERGRAPH_PREVIEW_MAIN)");
                s.AppendLine("{0} {1} = UnityBuildTexture2DStruct(_TerrainPreviewTexture);", outputTextureSlotType, GetVariableNameForSlot(OutputTextureId));
                s.AppendLine("#else");
                s.AppendLine("{0} {1} = TerrainBuildUnityTexture2DStruct({2}, {3});", outputTextureSlotType, GetVariableNameForSlot(OutputTextureId), textureName, indexValue);
                s.AppendLine("#endif");
            }

            switch (m_TextureType)
            {
                case TextureType.DiffuseMap:
                    if (Any(availableEdge)) TextureAvailable("_Splat");
                    if (Any(textureEdge)) TextureOutput("_Splat");
                    break;
                case TextureType.NormalMap:
                    if (Any(availableEdge)) TextureAvailable("_Normal");
                    if (Any(textureEdge)) TextureOutput("_Normal");
                    break;
                case TextureType.MaskMap:
                    if (Any(availableEdge)) TextureAvailable("_Mask");
                    if (Any(textureEdge)) TextureOutput("_Mask");
                    break;
                case TextureType.LayerMasks:
                    if (Any(availableEdge)) TextureAvailable("_Control");
                    if (Any(textureEdge))
                    {
                        s.AppendLine("#if defined(SHADERGRAPH_PREVIEW) || defined(SHADERGRAPH_PREVIEW_MAIN)");
                        s.AppendLine("{0} {1} = UnityBuildTexture2DStruct(_TerrainPreviewTexture);", outputTextureSlotType, GetVariableNameForSlot(OutputTextureId));
                        s.AppendLine("#else");
                        s.AppendLine("{0} {1} = TerrainBuildUnityTextureControl({2});", outputTextureSlotType, GetVariableNameForSlot(OutputTextureId), indexValue);
                        s.AppendLine("#endif");
                    }
                    break;
                case TextureType.Holes:
                    if (Any(availableEdge)) TextureAvailable("_Holes");
                    if (Any(textureEdge))
                    {
                        s.AppendLine("#if defined(SHADERGRAPH_PREVIEW) || defined(SHADERGRAPH_PREVIEW_MAIN)");
                        s.AppendLine("{0} {1} = UnityBuildTexture2DStruct(_TerrainPreviewTexture);", outputTextureSlotType, GetVariableNameForSlot(OutputTextureId));
                        s.AppendLine("#else");
                        s.AppendLine("{0} {1} = TerrainBuildUnityTextureHoles({2});", outputTextureSlotType, GetVariableNameForSlot(OutputTextureId), indexValue);
                        s.AppendLine("#endif");
                    }
                    break;
            }

        }

        private void GenerateAdditionalParameterOutputs(ShaderStringBuilder s, GenerationMode generationMode, string indexValue)
        {
            var hasNormalScaleEdge = Any(owner.GetEdges(FindOutputSlot<MaterialSlot>(NormalScaleId).slotReference));
            var hasMetallicDefaultEdge = Any(owner.GetEdges(FindOutputSlot<MaterialSlot>(MetallicDefaultId).slotReference));
            var hasSmoothnessDefaultEdge = Any(owner.GetEdges(FindOutputSlot<MaterialSlot>(SmoothnessDefaultId).slotReference));
            var hasDiffuseRemapEdge = Any(owner.GetEdges(FindOutputSlot<MaterialSlot>(DiffuseRemapId).slotReference));
            var hasOpacityAsDensityEdge = Any(owner.GetEdges(FindOutputSlot<MaterialSlot>(OpacityAsDensityId).slotReference));
            var hasMaskRemapOffsetEdge = Any(owner.GetEdges(FindOutputSlot<MaterialSlot>(MaskRemapOffsetId).slotReference));
            var hasMaskRemapScaleEdge = Any(owner.GetEdges(FindOutputSlot<MaterialSlot>(MaskRemapScaleId).slotReference));
            if (generationMode.IsPreview() || m_TextureType == TextureType.LayerMasks)
            {
                if (hasNormalScaleEdge) s.AppendLine("$precision {0} = 1;", GetVariableNameForSlot(NormalScaleId));
                if (hasMetallicDefaultEdge) s.AppendLine("$precision {0} = 0;", GetVariableNameForSlot(MetallicDefaultId));
                if (hasSmoothnessDefaultEdge) s.AppendLine("$precision {0} = 0.5f;", GetVariableNameForSlot(SmoothnessDefaultId));
                if (hasDiffuseRemapEdge) s.AppendLine("$precision3 {0} = $precision3(1,1,1);", GetVariableNameForSlot(DiffuseRemapId));
                if (hasOpacityAsDensityEdge) s.AppendLine("$precision {0} = 1;", GetVariableNameForSlot(OpacityAsDensityId));
                if (hasMaskRemapOffsetEdge) s.AppendLine("$precision4 {0} = $precision4(0,0,0,0);", GetVariableNameForSlot(MaskRemapOffsetId));
                if (hasMaskRemapScaleEdge) s.AppendLine("$precision4 {0} = $precision4(1,1,1,1);", GetVariableNameForSlot(MaskRemapScaleId));
                return;
            }
            void AdditionalParameterOutput(string functionName, int outputId, string defaultValue, string precisionString = "", string channels = "")
            {
                s.AppendLine("#if defined(SHADERGRAPH_PREVIEW) || defined(SHADERGRAPH_PREVIEW_MAIN)");
                s.AppendLine("$precision{0} {1} = {2};", precisionString, GetVariableNameForSlot(outputId), defaultValue);
                s.AppendLine("#else");
                s.AppendLine("$precision{0} {1} = Unity_Terrain_{2}({3}){4};", precisionString, GetVariableNameForSlot(outputId), functionName, indexValue, channels);
                s.AppendLine("#endif");
            }
            if (hasNormalScaleEdge) AdditionalParameterOutput("NormalScale", NormalScaleId, "1");
            if (hasMetallicDefaultEdge) AdditionalParameterOutput("Metallic", MetallicDefaultId, "0");
            if (hasSmoothnessDefaultEdge) AdditionalParameterOutput("Smoothness", SmoothnessDefaultId, "0.5f");
            if (hasDiffuseRemapEdge) AdditionalParameterOutput("DiffuseRemapScale", DiffuseRemapId, "$precision3(1,1,1)", "3", ".rgb");
            if (hasOpacityAsDensityEdge) AdditionalParameterOutput("DiffuseRemapScale", OpacityAsDensityId, "1", "", ".w");
            if (hasMaskRemapOffsetEdge) AdditionalParameterOutput("MaskMapRemapOffset", MaskRemapOffsetId, "$precision4(0,0,0,0)", "4");
            if (hasMaskRemapScaleEdge) AdditionalParameterOutput("MaskMapRemapScale", MaskRemapScaleId, "$precision4(1,1,1,1)", "4");
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            registry.RequiresIncludePath("Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl");
            registry.RequiresIncludePath("Packages/com.unity.shadergraph/Editor/Generation/Targets/Terrain/Includes/TerrainTextureVariables.hlsl");
        }
    }
}
