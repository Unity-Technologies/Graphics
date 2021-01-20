using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Type acts as wrapper for post process passes. Can we be recreated and destroyed at any point during runtime with post process data.
    /// </summary>
    internal struct PostProcessPasses : IDisposable
    {
        ColorGradingLutPass m_ColorGradingLutPass;
        PostProcessPass m_PostProcessPass;
        PostProcessPass m_FinalPostProcessPass;

        RTHandle m_AfterPostProcessColor;
        RTHandle m_ColorGradingLut;

        PostProcessData m_RendererPostProcessData;
        PostProcessData m_CurrentPostProcessData;
        Material m_BlitMaterial;

        public ColorGradingLutPass colorGradingLutPass { get => m_ColorGradingLutPass; }
        public PostProcessPass postProcessPass { get => m_PostProcessPass; }
        public PostProcessPass finalPostProcessPass { get => m_FinalPostProcessPass; }
        public RTHandle afterPostProcessColor { get => m_AfterPostProcessColor; }
        public RTHandle colorGradingLut { get => m_ColorGradingLut; }

        public bool isCreated { get => m_CurrentPostProcessData != null; }

        static bool NeedsReAlloc(in RTHandle handle, in RenderTextureDescriptor desc)
        {
            return handle.rt.graphicsFormat != desc.graphicsFormat ||
                handle.rt.dimension != desc.dimension ||
                handle.rt.enableRandomWrite != desc.enableRandomWrite ||
                handle.rt.useMipMap != desc.useMipMap ||
                handle.rt.autoGenerateMips != desc.autoGenerateMips ||
                handle.rt.bindTextureMS != desc.bindMS ||
                handle.rt.useDynamicScale != desc.useDynamicScale ||
                handle.rt.memorylessMode != desc.memoryless;
        }

        public PostProcessPasses(PostProcessData rendererPostProcessData, Material blitMaterial)
        {
            m_ColorGradingLutPass = null;
            m_PostProcessPass = null;
            m_FinalPostProcessPass = null;
            m_CurrentPostProcessData = null;

            m_AfterPostProcessColor = RTHandles.Alloc(
                Vector2.one,
                depthBufferBits: DepthBits.None,
                colorFormat: GraphicsFormat.B10G11R11_UFloatPack32,
                filterMode: FilterMode.Point,
                wrapMode: TextureWrapMode.Clamp,
                dimension: TextureDimension.Tex2D,
                enableRandomWrite: false,
                useMipMap: false,
                autoGenerateMips: true,
                bindTextureMS: false,
                useDynamicScale: false,
                memoryless: RenderTextureMemoryless.None,
                name: "_AfterPostProcessTexture");
            m_ColorGradingLut = RTHandles.Alloc(Shader.PropertyToID("_InternalGradingLut"), "_InternalGradingLut");

            m_RendererPostProcessData = rendererPostProcessData;
            m_BlitMaterial = blitMaterial;


            Recreate(rendererPostProcessData);
        }

        public void Setup(in RenderTextureDescriptor cameraTargetDescriptor)
        {
            if (NeedsReAlloc(m_AfterPostProcessColor, cameraTargetDescriptor))
            {
                m_AfterPostProcessColor.Release();
                m_AfterPostProcessColor = RTHandles.Alloc(
                    Vector2.one,
                    filterMode: FilterMode.Point,
                    wrapMode: TextureWrapMode.Clamp,
                    depthBufferBits: DepthBits.None,
                    colorFormat: cameraTargetDescriptor.graphicsFormat,
                    dimension: cameraTargetDescriptor.dimension,
                    enableRandomWrite: cameraTargetDescriptor.enableRandomWrite,
                    useMipMap: cameraTargetDescriptor.useMipMap,
                    autoGenerateMips: cameraTargetDescriptor.autoGenerateMips,
                    bindTextureMS: cameraTargetDescriptor.bindMS,
                    useDynamicScale: cameraTargetDescriptor.useDynamicScale,
                    memoryless: cameraTargetDescriptor.memoryless,
                    name: m_AfterPostProcessColor.name);
            }
        }

        /// <summary>
        /// Recreates post process passes with supplied data. If already contains valid post process passes, they will be replaced by new ones.
        /// </summary>
        /// <param name="data">Resources used for creating passes. In case of the null, no passes will be created.</param>
        public void Recreate(PostProcessData data)
        {
            if (m_RendererPostProcessData)
                data = m_RendererPostProcessData;

            if (data == m_CurrentPostProcessData)
                return;

            if (m_CurrentPostProcessData != null)
            {
                m_ColorGradingLutPass?.Cleanup();
                m_PostProcessPass?.Cleanup();
                m_FinalPostProcessPass?.Cleanup();

                // We need to null post process passes to avoid using them
                m_ColorGradingLutPass = null;
                m_PostProcessPass = null;
                m_FinalPostProcessPass = null;
                m_CurrentPostProcessData = null;
            }

            if (data != null)
            {
                m_ColorGradingLutPass = new ColorGradingLutPass(RenderPassEvent.BeforeRenderingPrePasses, data);
                m_PostProcessPass = new PostProcessPass(RenderPassEvent.BeforeRenderingPostProcessing, data, m_BlitMaterial);
                m_FinalPostProcessPass = new PostProcessPass(RenderPassEvent.AfterRenderingPostProcessing, data, m_BlitMaterial);
                m_CurrentPostProcessData = data;
            }
        }

        public void Dispose()
        {
            // always dispose unmanaged resources
            m_ColorGradingLutPass?.Cleanup();
            m_PostProcessPass?.Cleanup();
            m_FinalPostProcessPass?.Cleanup();

            m_AfterPostProcessColor.Release();
            m_ColorGradingLut.Release();
        }
    }
}
