using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    public struct PostProcessParams
    {
        public Material blitMaterial;
        public GraphicsFormat requestHDRFormat;

        public static PostProcessParams GetDefault()
        {
            PostProcessParams ppParams;
            ppParams.blitMaterial = null;
            ppParams.requestHDRFormat = GraphicsFormat.B10G11R11_UFloatPack32;
            return ppParams;
        }
    }

    /// <summary>
    /// Type acts as wrapper for post process passes. Can we be recreated and destroyed at any point during runtime with post process data.
    /// </summary>
    internal struct PostProcessPasses : IDisposable
    {
        ColorGradingLutPass m_ColorGradingLutPass;
        PostProcessPass m_PostProcessPass;
        PostProcessPass m_FinalPostProcessPass;

        RenderTargetHandle m_AfterPostProcessColor;
        RenderTargetHandle m_ColorGradingLut;

        PostProcessData m_RendererPostProcessData;
        PostProcessData m_CurrentPostProcessData;
        Material m_BlitMaterial;

        public ColorGradingLutPass colorGradingLutPass { get => m_ColorGradingLutPass; }
        public PostProcessPass postProcessPass { get => m_PostProcessPass; }
        public PostProcessPass finalPostProcessPass { get => m_FinalPostProcessPass; }
        public RenderTargetHandle afterPostProcessColor { get => m_AfterPostProcessColor; }
        public RenderTargetHandle colorGradingLut { get => m_ColorGradingLut; }

        public bool isCreated { get => m_CurrentPostProcessData != null; }

        private PostProcessPasses(PostProcessData rendererPostProcessData)
        {
            m_ColorGradingLutPass = null;
            m_PostProcessPass = null;
            m_FinalPostProcessPass = null;
            m_AfterPostProcessColor = new RenderTargetHandle();
            m_ColorGradingLut = new RenderTargetHandle();
            m_CurrentPostProcessData = null;
            m_BlitMaterial = null;

            m_AfterPostProcessColor.Init("_AfterPostProcessTexture");
            m_ColorGradingLut.Init("_InternalGradingLut");
            m_RendererPostProcessData = rendererPostProcessData;
        }

        public PostProcessPasses(PostProcessData rendererPostProcessData, ref PostProcessParams postProcessParams) : this(rendererPostProcessData)
        {
            m_BlitMaterial = postProcessParams.blitMaterial;
            Recreate(rendererPostProcessData, ref postProcessParams);
        }

        [Obsolete]
        public PostProcessPasses(PostProcessData rendererPostProcessData, Material blitMaterial) : this(rendererPostProcessData)
        {
            m_BlitMaterial = blitMaterial;
            var ppParams = new PostProcessParams { blitMaterial = blitMaterial, requestHDRFormat = GraphicsFormat.B10G11R11_UFloatPack32 };
            Recreate(rendererPostProcessData, ref ppParams);
        }


        /// <summary>
        /// Recreates post process passes with supplied data. If already contains valid post process passes, they will be replaced by new ones.
        /// </summary>
        /// <param name="data">Resources used for creating passes. In case of the null, no passes will be created.</param>
        /// <param name="ppParams">Resources used for creating passes. In case of the null, no passes will be created.</param>
        public void Recreate(PostProcessData data, ref PostProcessParams ppParams)
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
                m_PostProcessPass = new PostProcessPass(RenderPassEvent.BeforeRenderingPostProcessing, data, ref ppParams);
                m_FinalPostProcessPass = new PostProcessPass(RenderPassEvent.AfterRenderingPostProcessing, data, ref ppParams);
                m_CurrentPostProcessData = data;
            }
        }

        [Obsolete]
        public void Recreate(PostProcessData data)
        {
            var ppParams = new PostProcessParams { blitMaterial = null, requestHDRFormat = GraphicsFormat.B10G11R11_UFloatPack32 };
            Recreate(data, ref ppParams);
        }

        public void Dispose()
        {
            // always dispose unmanaged resources
            m_ColorGradingLutPass?.Cleanup();
            m_PostProcessPass?.Cleanup();
            m_FinalPostProcessPass?.Cleanup();
        }
    }
}
