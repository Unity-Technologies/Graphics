using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Rendering.Universal;
using System.Reflection;
using System.Linq;
using UnityEngine.XR;

namespace UnityEditor.Rendering.Universal
{
    [SRPFilter(typeof(UniversalRenderPipeline))]
    [Title("Input", "Universal", "URP Sample Buffer")]
    sealed class UniversalSampleBufferNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction, IMayRequireScreenPosition, IMayRequireNDCPosition
    {
        const string k_ScreenPositionSlotName = "UV";
        const string k_OutputSlotName = "Output";

        const int k_ScreenPositionSlotId = 0;
        const int k_OutputSlotId = 2;

        public enum BufferType
        {
            NormalWorldSpace,
            MotionVectors,
            BlitSource,
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

        public override string documentationURL => Documentation.GetPageLink(Documentation.packageName, "SGNode-Universal-Sample-Buffer");

        public UniversalSampleBufferNode()
        {
            name = "URP Sample Buffer";
            synonyms = new string[] { "normal", "motion vector", "blit" };
            UpdateNodeAfterDeserialization();
        }

        public override bool hasPreview { get { return true; } }
        public override PreviewMode previewMode => PreviewMode.Preview2D;

        int channelCount;

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new ScreenPositionMaterialSlot(k_ScreenPositionSlotId, k_ScreenPositionSlotName, k_ScreenPositionSlotName, ScreenSpaceType.Default));

            switch (bufferType)
            {
                case BufferType.NormalWorldSpace:
                    AddSlot(new Vector3MaterialSlot(k_OutputSlotId, k_OutputSlotName, k_OutputSlotName, SlotType.Output, Vector3.zero, ShaderStageCapability.Fragment));
                    channelCount = 3;
                    break;
                case BufferType.MotionVectors:
                    AddSlot(new Vector2MaterialSlot(k_OutputSlotId, k_OutputSlotName, k_OutputSlotName, SlotType.Output, Vector2.zero, ShaderStageCapability.Fragment));
                    channelCount = 2;
                    break;
                case BufferType.BlitSource:
                    AddSlot(new ColorRGBAMaterialSlot(k_OutputSlotId, k_OutputSlotName, k_OutputSlotName, SlotType.Output, Color.black, ShaderStageCapability.Fragment));
                    channelCount = 4;
                    break;
            }

            RemoveSlotsNameNotMatching(new[]
            {
                k_ScreenPositionSlotId,
                k_OutputSlotId,
            });
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            if (generationMode.IsPreview())
                return;
        }

        string GetFunctionName() => $"Unity_Universal_SampleBuffer_{bufferType}_$precision";

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            // Preview SG doesn't have access to render pipeline buffer
            if (!generationMode.IsPreview())
            {
                registry.ProvideFunction(GetFunctionName(), s =>
                {
                    if (bufferType == BufferType.MotionVectors)
                        s.AppendLine("TEXTURE2D_X(_MotionVectorTexture);");

                    if (bufferType == BufferType.BlitSource)
                    {
                        s.AppendLine("TEXTURE2D_X(_BlitTexture);");
                    }

                    s.AppendLine("$precision{1} {0}($precision2 uv)", GetFunctionName(), channelCount);
                    using (s.BlockScope())
                    {
                        switch (bufferType)
                        {
                            case BufferType.NormalWorldSpace:
                                s.AppendLine("return SHADERGRAPH_SAMPLE_SCENE_NORMAL(uv);");
                                break;
                            case BufferType.MotionVectors:
                                s.AppendLine("uint2 pixelCoords = uint2(uv * _ScreenSize.xy);");
                                s.AppendLine($"return LOAD_TEXTURE2D_X_LOD(_MotionVectorTexture, pixelCoords, 0).xy;");
                                break;
                            case BufferType.BlitSource:
                                s.AppendLine("uint2 pixelCoords = uint2(uv * _ScreenSize.xy);");
                                s.AppendLine($"return LOAD_TEXTURE2D_X_LOD(_BlitTexture, pixelCoords, 0);");
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
                    s.AppendLine("$precision{1} {0}($precision2 uv)", GetFunctionName(), channelCount);
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
                            case BufferType.BlitSource:
                            default:
                                s.AppendLine("return 0.0;");
                                break;
                        }
                    }
                });
            }
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            string uv = GetSlotValue(k_ScreenPositionSlotId, generationMode);
            sb.AppendLine($"$precision{channelCount} {GetVariableNameForSlot(k_OutputSlotId)} = {GetFunctionName()}({uv}.xy);");
        }

        public bool RequiresNDCPosition(ShaderStageCapability stageCapability = ShaderStageCapability.All) => true;
        public bool RequiresScreenPosition(ShaderStageCapability stageCapability = ShaderStageCapability.All) => true;
    }
}
