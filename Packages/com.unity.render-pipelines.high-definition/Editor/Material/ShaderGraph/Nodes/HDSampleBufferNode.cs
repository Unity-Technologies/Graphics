using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Rendering.HighDefinition;
using System.Reflection;
using System.Linq;

namespace UnityEditor.Rendering.HighDefinition
{
    [SRPFilter(typeof(HDRenderPipeline))]
    [Title("Input", "High Definition Render Pipeline", "HD Sample Buffer")]
    sealed class HDSampleBufferNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction, IMayRequireScreenPosition, IMayRequireDepthTexture, IMayRequireNDCPosition
    {
        const string k_ScreenPositionSlotName = "UV";
        const string k_ThicknessLayerIDSlotName = "Layer Mask";
        const string k_OutputSlotName = "Output";
        const string k_OutputThicknessSlotName = "Thickness";
        const string k_OutputOverlapCountSlotName = "Overlap Count";
        const string k_OutputDistanceSlotName = "Distance";

        const int k_ScreenPositionSlotId = 0;
        const int k_ThicknessLayerIDSlotId = 1;
        const int k_OutputSlotId = 2;
        const int k_OutputThicknessSlotId = 3;
        const int k_OutputOverlapSlotId = 4;
        const int k_OutputDistanceSlotId = 5;

        public enum BufferType
        {
            NormalWorldSpace,
            Smoothness,
            MotionVectors,
            IsSky,
            PostProcessInput,
            RenderingLayerMask,
            Thickness,
            IsUnderWater,
        }

        [SerializeField]
        private BufferType m_BufferType = BufferType.NormalWorldSpace;

        [EnumControl("Source Buffer")]
        public BufferType bufferType
        {
            get { return m_BufferType; }
            set
            {
                if (m_BufferType == value)
                    return;

                m_BufferType = value;
                UpdateNodeAfterDeserialization();
                Dirty(ModificationScope.Graph);
            }
        }

        public override string documentationURL => NodeUtils.GetDocumentationString("HD-Sample-Buffer");


        public static List<HDSampleBufferNode> nodeList = new();

        public HDSampleBufferNode()
        {
            name = "HD Sample Buffer";
            synonyms = new string[] { "normal", "motion vector", "smoothness", "postprocessinput", "issky", "thickness", "underwater" };
            UpdateNodeAfterDeserialization();

            nodeList.Add(this);
        }

        ~HDSampleBufferNode()
        {
            nodeList.Remove(this);
        }

        public override bool hasPreview { get { return true; } }
        public override PreviewMode previewMode => PreviewMode.Preview2D;

        int channelCount;

        public sealed override void UpdateNodeAfterDeserialization()
        {
            var addedSlots = new List<int>();

            var last0 = AddSlot(new ScreenPositionMaterialSlot(k_ScreenPositionSlotId, k_ScreenPositionSlotName, k_ScreenPositionSlotName, ScreenSpaceType.Default));
            addedSlots.Add(last0.id);

            switch (bufferType)
            {
                case BufferType.NormalWorldSpace:
                    {
                        var last = AddSlot(new Vector3MaterialSlot(k_OutputSlotId, k_OutputSlotName, k_OutputSlotName, SlotType.Output, Vector3.zero, ShaderStageCapability.Fragment));
                        addedSlots.Add(last.id);
                        channelCount = 3;
                    }
                    break;
                case BufferType.Smoothness:
                    {
                        var last = AddSlot(new Vector1MaterialSlot(k_OutputSlotId, k_OutputSlotName, k_OutputSlotName, SlotType.Output, 0, ShaderStageCapability.Fragment));
                        addedSlots.Add(last.id);
                        channelCount = 1;
                    }
                    break;
                case BufferType.MotionVectors:
                    {
                        var last = AddSlot(new Vector2MaterialSlot(k_OutputSlotId, k_OutputSlotName, k_OutputSlotName, SlotType.Output, Vector2.zero, ShaderStageCapability.Fragment));
                        addedSlots.Add(last.id);
                        channelCount = 2;
                    }
                    break;
                case BufferType.IsSky:
                    {
                        var last = AddSlot(new Vector1MaterialSlot(k_OutputSlotId, k_OutputSlotName, k_OutputSlotName, SlotType.Output, 0, ShaderStageCapability.Fragment));
                        addedSlots.Add(last.id);
                        channelCount = 1;
                    }
                    break;
                case BufferType.PostProcessInput:
                    {
                        var last = AddSlot(new ColorRGBAMaterialSlot(k_OutputSlotId, k_OutputSlotName, k_OutputSlotName, SlotType.Output, Color.black, ShaderStageCapability.Fragment));
                        addedSlots.Add(last.id);
                        channelCount = 4;
                    }
                    break;
                case BufferType.RenderingLayerMask:
                    {
                        var last = AddSlot(new Vector1MaterialSlot(k_OutputSlotId, k_OutputSlotName, k_OutputSlotName, SlotType.Output, 0, ShaderStageCapability.Fragment));
                        addedSlots.Add(last.id);
                        channelCount = 1;
                    }
                    break;
                case BufferType.Thickness:
                    {
                        var lastMat = AddSlot(new Vector1MaterialSlot(k_ThicknessLayerIDSlotId, k_ThicknessLayerIDSlotName, k_ThicknessLayerIDSlotName, SlotType.Input, 0.0f, ShaderStageCapability.Fragment));
                        addedSlots.Add(lastMat.id);
                        var last = AddSlot(new Vector1MaterialSlot(k_OutputThicknessSlotId, k_OutputThicknessSlotName, k_OutputThicknessSlotName, SlotType.Output, 0.0f, ShaderStageCapability.Fragment));
                        addedSlots.Add(last.id);
                        last = AddSlot(new Vector1MaterialSlot(k_OutputOverlapSlotId, k_OutputOverlapCountSlotName, k_OutputOverlapCountSlotName, SlotType.Output, 0.0f, ShaderStageCapability.Fragment));
                        addedSlots.Add(last.id);
                        channelCount = 2;
                    }
                    break;
                case BufferType.IsUnderWater:
                    {
                        var last = AddSlot(new BooleanMaterialSlot(k_OutputSlotId, k_OutputSlotName, k_OutputSlotName, SlotType.Output, false, ShaderStageCapability.Fragment));
                        addedSlots.Add(last.id);
                        var distance = AddSlot(new Vector1MaterialSlot(k_OutputDistanceSlotId, k_OutputDistanceSlotName, k_OutputDistanceSlotName, SlotType.Output, 0, ShaderStageCapability.Fragment));
                        addedSlots.Add(distance.id);
                        channelCount = 1;
                    }
                    break;
            }

            RemoveSlotsNameNotMatching(addedSlots, supressWarnings: true);
        }

        string GetFunctionName() => $"Unity_HDRP_SampleBuffer_{bufferType}_$precision";

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            // Preview SG doesn't have access to HDRP depth buffer
            if (!generationMode.IsPreview())
            {
                registry.RequiresIncludePath("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl");
                registry.RequiresIncludePath("Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Builtin/BuiltinData.hlsl");

                if (bufferType == BufferType.IsUnderWater)
                    registry.RequiresIncludePath("Packages/com.unity.render-pipelines.high-definition/Runtime/Water/Shaders/UnderWaterUtilities.hlsl");

                registry.ProvideFunction(GetFunctionName(), s =>
                {
                    if (bufferType == BufferType.PostProcessInput)
                    {
                        // Declare post process input here because the property collector don't support TEXTURE_X type
                        s.AppendLine($"TEXTURE2D_X({nameof(HDShaderIDs._CustomPostProcessInput)});");
                    }

                    s.AppendLine("$precision{1} {0}($precision2 uv, int layerID)", GetFunctionName(), channelCount);
                    using (s.BlockScope())
                    {
                        switch (bufferType)
                        {
                            case BufferType.NormalWorldSpace:
                                s.AppendLine("uint2 pixelCoords = uint2(uv * _ScreenSize.xy);");
                                s.AppendLine("NormalData normalData;");
                                s.AppendLine("DecodeFromNormalBuffer(pixelCoords, normalData);");
                                s.AppendLine("return normalData.normalWS;");
                                break;
                            case BufferType.Smoothness:
                                s.AppendLine("uint2 pixelCoords = uint2(uv * _ScreenSize.xy);");
                                s.AppendLine("NormalData normalData;");
                                s.AppendLine("DecodeFromNormalBuffer(pixelCoords, normalData);");
                                s.AppendLine("return IsSky(pixelCoords) ? 1 : RoughnessToPerceptualSmoothness(PerceptualRoughnessToRoughness(normalData.perceptualRoughness));");
                                break;
                            case BufferType.MotionVectors:
                                s.AppendLine("uint2 pixelCoords = uint2(uv * _ScreenSize.xy);");
                                s.AppendLine($"float4 motionVecBufferSample = LOAD_TEXTURE2D_X_LOD(_CameraMotionVectorsTexture, pixelCoords, 0);");
                                s.AppendLine("float2 motionVec;");
                                s.AppendLine("DecodeMotionVector(motionVecBufferSample, motionVec);");
                                s.AppendLine("return motionVec;");
                                break;
                            case BufferType.IsSky:
                                s.AppendLine("return IsSky(uv) ? 1 : 0;");
                                break;
                            case BufferType.PostProcessInput:
                                s.AppendLine("uint2 pixelCoords = uint2(uv * _ScreenSize.xy);");
                                s.AppendLine("return LOAD_TEXTURE2D_X_LOD(_CustomPostProcessInput, pixelCoords, 0);");
                                break;
                            case BufferType.RenderingLayerMask:
                                s.AppendLine("uint2 pixelCoords = uint2(uv * _ScreenSize.xy);");
                                s.AppendLine("return _EnableRenderingLayers ? UnpackMeshRenderingLayerMask(LOAD_TEXTURE2D_X_LOD(_RenderingLayerMaskTexture, pixelCoords, 0)) : 0;");
                                break;
                            case BufferType.Thickness:
                                s.AppendLine(GetRayTracingError());
                                s.AppendLine("return SampleThickness(uv.xy, layerID);");
                                break;
                            case BufferType.IsUnderWater:
                                s.AppendLine("uint2 pixelCoords = uint2(uv * _ScreenSize.xy);");
                                s.AppendLine("return _UnderWaterSurfaceIndex != -1 ? GetUnderWaterDistance(pixelCoords) : 1.0f;");
                                break;
                            default:
                                s.AppendLine("return 0.0;");
                                break;
                        }
                    }
                });
            }
            else
            {
                registry.ProvideFunction(GetFunctionName(), s =>
                {
                    s.AppendLine("$precision{1} {0}($precision2 uv, int layerID)", GetFunctionName(), channelCount);
                    using (s.BlockScope())
                    {
                        switch (bufferType)
                        {
                            case BufferType.NormalWorldSpace:
                                s.AppendLine("return LatlongToDirectionCoordinate(uv);");
                                break;
                            case BufferType.MotionVectors:
                                s.AppendLine("return uv * 2 - 1;");
                                break;
                            case BufferType.Smoothness:
                                s.AppendLine("return uv.x;");
                                break;
                            case BufferType.Thickness:
                                // Thickness of a centered sphere seen from an infinite point of view
                                s.AppendLine("return pow(abs(1.0f - saturate(dot(uv * 2 - 1, uv * 2 - 1))), 2.2f);");
                                break;
                            case BufferType.IsUnderWater:
                                s.AppendLine("return uv.y * 2 - 1;");
                                break;
                            default:
                                s.AppendLine("return 0.0f;");
                                break;
                        }
                    }
                });
            }
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            string uv = GetSlotValue(k_ScreenPositionSlotId, generationMode);
            if (bufferType == BufferType.Thickness)
            {
                string layerID = GetSlotValue(k_ThicknessLayerIDSlotId, generationMode);
                sb.AppendLine($"$precision2 {GetVariableNameForSlot(k_OutputThicknessSlotId)}_Value = {GetFunctionName()}({uv}.xy, (int){layerID});");
                sb.AppendLine($"$precision {GetVariableNameForSlot(k_OutputThicknessSlotId)} = {GetVariableNameForSlot(k_OutputThicknessSlotId)}_Value.x;");
                sb.AppendLine($"$precision {GetVariableNameForSlot(k_OutputOverlapSlotId)} = {GetVariableNameForSlot(k_OutputThicknessSlotId)}_Value.y;");
            }
            else if (bufferType == BufferType.IsUnderWater)
            {
                sb.AppendLine($"$precision {GetVariableNameForSlot(k_OutputSlotId)}_Value = {GetFunctionName()}({uv}.xy, 0);");
                sb.AppendLine($"$precision {GetVariableNameForSlot(k_OutputSlotId)} = {GetVariableNameForSlot(k_OutputSlotId)}_Value <= 0.0f;");
                sb.AppendLine($"$precision {GetVariableNameForSlot(k_OutputDistanceSlotId)} = {GetVariableNameForSlot(k_OutputSlotId)}_Value;");
            }
            else
            {
                sb.AppendLine($"$precision{channelCount} {GetVariableNameForSlot(k_OutputSlotId)} = {GetFunctionName()}({uv}.xy, 0);");
            }
        }

        public bool RequiresDepthTexture(ShaderStageCapability stageCapability) => true;
        public bool RequiresNDCPosition(ShaderStageCapability stageCapability = ShaderStageCapability.All) => true;
        public bool RequiresScreenPosition(ShaderStageCapability stageCapability = ShaderStageCapability.All) => true;

        #region Warning Badge
        static readonly Dictionary<BufferType, ShaderMessage> s_TypeToMessage = new()
        {
            { BufferType.RenderingLayerMask, new ShaderMessage("Rendering Layer Mask Buffer is not enabled in the HDRP Asset. This will not work.", ShaderCompilerMessageSeverity.Warning) },
            { BufferType.Thickness, new ShaderMessage("Compute Thickness is not enabled in the HDRP Asset. This will not work.", ShaderCompilerMessageSeverity.Warning) },
            { BufferType.IsUnderWater, new ShaderMessage("Water is not enabled in the HDRP Asset. This will not work.", ShaderCompilerMessageSeverity.Warning) },
        };

        public override void ValidateNode()
        {
            if ((bufferType == BufferType.RenderingLayerMask && HDRenderPipeline.currentAsset?.currentPlatformRenderPipelineSettings.renderingLayerMaskBuffer == false) ||
                (bufferType == BufferType.Thickness && HDRenderPipeline.currentAsset?.currentPlatformRenderPipelineSettings.supportComputeThickness == false) ||
                (bufferType == BufferType.IsUnderWater && HDRenderPipeline.currentAsset?.currentPlatformRenderPipelineSettings.supportWater == false))
                owner.messageManager?.AddOrAppendError(owner, objectId, s_TypeToMessage[bufferType]);
        }

        private void UpdateWarningBadge(BufferType bufferType, bool supported)
        {
            if (owner == null) return;

            if (!supported && this.bufferType == bufferType)
                owner.messageManager?.AddOrAppendError(owner, objectId, s_TypeToMessage[bufferType]);
            else
                owner.ClearErrorsForNode(this);
        }

        internal static void UpdateWarningBadges(BufferType bufferType, bool supported)
        {
            foreach (var node in nodeList)
            {
                if (node != null)
                    node.UpdateWarningBadge(bufferType, supported);
            }

            EditorApplication.delayCall += () => {
                foreach (var node in nodeList)
                {
                    if (node != null && node.owner?.owner != null)
                        node.owner.owner.Validate();
                }
            };
        }
        #endregion
    }
}
