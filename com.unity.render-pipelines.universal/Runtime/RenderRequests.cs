using System;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    internal class RenderRequestRendererData : ScriptableRendererData
    {
        protected override ScriptableRenderer Create()
        {
            switch (request.mode)
            {
                case Camera.RenderRequestMode.SelectionMask:
                    return new RenderRequestSelectionMaskRenderer(this);
                default:
                    return new RenderRequestRenderer(this);
            }
        }

        public Camera.RenderRequest request { get; set; }
    }

    internal class RenderRequestRenderer : ScriptableRenderer
    {
        DrawObjectsPass m_RenderOpaqueForwardPass;
        DrawObjectsPass m_RenderTransparentForwardPass;

        LayerMask m_OpaqueLayerMask = -1;
        LayerMask m_TransparentLayerMask = -1;
        StencilState m_DefaultStencilState = StencilState.defaultValue;

        public RenderRequestRenderer(RenderRequestRendererData data) : base(data)
        {
            var shaderTags = new[] {new ShaderTagId("DataExtraction")};

            m_RenderOpaqueForwardPass = new DrawObjectsPass("Render Opaques", shaderTags, true, RenderPassEvent.BeforeRenderingOpaques, RenderQueueRange.opaque, m_OpaqueLayerMask, m_DefaultStencilState, 0 );
            m_RenderTransparentForwardPass = new DrawObjectsPass("Render Transparents", shaderTags, false, RenderPassEvent.BeforeRenderingTransparents, RenderQueueRange.transparent, m_TransparentLayerMask, m_DefaultStencilState, 0);

            List<Tuple<string, int>> values = new List<Tuple<string, int>>
            {
                new Tuple<string, int>("UNITY_DataExtraction_Mode", (int)data.request.mode),
                new Tuple<string, int>("UNITY_DataExtraction_Space", (int)data.request.outputSpace),
            };

            m_RenderOpaqueForwardPass.SetAdditionalValues(values);
            m_RenderTransparentForwardPass.SetAdditionalValues(values);
        }

        /// <inheritdoc />
        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            EnqueuePass(m_RenderOpaqueForwardPass);
            EnqueuePass(m_RenderTransparentForwardPass);
        }

        public override void FinishRendering(CommandBuffer cmd)
        {
            base.FinishRendering(cmd);
            cmd.SetGlobalInt("UNITY_DataExtraction_Mode", 0);
            cmd.SetGlobalInt("UNITY_DataExtraction_Space", 0);
        }
    }

    internal class RenderRequestSelectionMaskRenderer : ScriptableRenderer
    {
        DrawObjectsPass m_RenderOpaqueForwardPassZDisabled;
        DrawObjectsPass m_RenderTransparentForwardPassZDisabled;

        LayerMask m_OpaqueLayerMask = -1;
        LayerMask m_TransparentLayerMask = -1;
        StencilState m_DefaultStencilState = StencilState.defaultValue;

        private const float kSelectionZFudgeFactor = -0.02f;

        public RenderRequestSelectionMaskRenderer(RenderRequestRendererData data) : base(data)
        {
            Debug.Assert(data.request.mode == Camera.RenderRequestMode.SelectionMask, "RenderRequestSelectionMaskRenderer should only be used with RenderRequestMode.SelectionMask");

            var shaderTags = new[] {new ShaderTagId("DataExtraction")};

            // There is no Z buffer for selection mask, use blending to make sure "unoccluded" (G = 1) values
            // always remain on top when both occluded and unoccluded objects overlap.

            m_RenderOpaqueForwardPassZDisabled = new DrawObjectsPass("Render Opaques (Z disabled)", shaderTags, true, RenderPassEvent.BeforeRenderingOpaques, RenderQueueRange.opaque, m_OpaqueLayerMask, m_DefaultStencilState, 0 );
            m_RenderTransparentForwardPassZDisabled = new DrawObjectsPass("Render Transparents (Z disabled)", shaderTags, false, RenderPassEvent.BeforeRenderingTransparents, RenderQueueRange.transparent, m_TransparentLayerMask, m_DefaultStencilState, 0);

            var state = m_RenderOpaqueForwardPassZDisabled.RenderStateBlock;
            state.rasterState = new RasterState(CullMode.Back, 0, kSelectionZFudgeFactor);
            state.blendState = new BlendState
            {
                blendState0 = new RenderTargetBlendState
                {
                    colorBlendOperation = BlendOp.Max,
                    sourceColorBlendMode = BlendMode.One,
                    destinationColorBlendMode = BlendMode.One,
                    alphaBlendOperation = BlendOp.Max,
                    sourceAlphaBlendMode = BlendMode.One,
                    destinationAlphaBlendMode = BlendMode.One,
                    writeMask = ColorWriteMask.Green | ColorWriteMask.Blue | ColorWriteMask.Alpha,
                }
            };
            state.depthState = new DepthState {compareFunction = CompareFunction.Always, writeEnabled = false};
            state.mask = RenderStateMask.Everything;
            m_RenderOpaqueForwardPassZDisabled.RenderStateBlock = state;
            m_RenderTransparentForwardPassZDisabled.RenderStateBlock = state;

            List<Tuple<string, int>> values = new List<Tuple<string, int>>
            {
                new Tuple<string, int>("UNITY_DataExtraction_Mode", (int)data.request.mode),
                new Tuple<string, int>("UNITY_DataExtraction_Space", (int)data.request.outputSpace),
            };

            m_RenderOpaqueForwardPassZDisabled.SetAdditionalValues(values);
            m_RenderTransparentForwardPassZDisabled.SetAdditionalValues(values);
        }

        /// <inheritdoc />
        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            EnqueuePass(m_RenderOpaqueForwardPassZDisabled);
            EnqueuePass(m_RenderTransparentForwardPassZDisabled);
        }

        public override void FinishRendering(CommandBuffer cmd)
        {
            base.FinishRendering(cmd);
            cmd.SetGlobalInt("UNITY_DataExtraction_Mode", 0);
            cmd.SetGlobalInt("UNITY_DataExtraction_Space", 0);
        }
    }
}
