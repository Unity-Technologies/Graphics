using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

#if UNITY_EDITOR
    using UnityEditor;
#endif

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    internal class Default2DRendererSetup : LightweightRendererSetup
    {
        [Serializable]
        public class RenderTextureInfo
        {
            public int width = 512;
            public int height = 512;
            public int msaaSamples = 1;
            public FilterMode filterMode = FilterMode.Bilinear;

            // This probably needs to be changed...
            public static implicit operator RenderTextureDescriptor(RenderTextureInfo info)
            {
                RenderTextureDescriptor desc = new RenderTextureDescriptor();
                desc.width = info.width;
                desc.height = info.height;
                desc.msaaSamples = info.msaaSamples;
                desc.dimension = TextureDimension.Tex2D;
                desc.colorFormat = RenderTextureFormat.RGB111110Float;
                desc.depthBufferBits = 0;
                return desc;
            }
        }

        public RenderTextureInfo m_AmbientRenderTextureInfo;
        public RenderTextureInfo m_SpecularRenderTextureInfo;
        public RenderTextureInfo m_RimRenderTextureInfo;
        public Color m_AmbientDefaultColor;

        private RenderTextureFormat m_RenderTextureFormatToUse;

        Render2DLightingPass m_Render2DLightingPass;

        //Render2DFallbackPass   m_Render2DFallbackPass;

        [NonSerialized]
        private bool m_Initialized = false;

        //static void SetShaderGlobals(CommandBuffer cmdBuffer)
        //{

        //    cmdBuffer.SetGlobalColor("_AmbientColor", m_DefaultAmbientColor);
        //    cmdBuffer.SetGlobalColor("_RimColor", Color.black);
        //    cmdBuffer.SetGlobalColor("_SpecularColor", Color.black);

        //    cmdBuffer.SetGlobalTexture("_SpecularLightingTex", m_SpecularLightRTHandle);
        //    cmdBuffer.SetGlobalTexture("_AmbientLightingTex", m_AmbientLightRTHandle);
        //    cmdBuffer.SetGlobalTexture("_RimLightingTex", m_FullScreenRimLightTexture);
        //    cmdBuffer.SetGlobalTexture("_ShadowTex", m_FullScreenShadowTexture);
        //}

        private void Init()
        {
            if (m_Initialized)
                return;

            m_RenderTextureFormatToUse = RenderTextureFormat.ARGB32;
            if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf))
                m_RenderTextureFormatToUse = RenderTextureFormat.ARGBHalf;
            else if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat))
                m_RenderTextureFormatToUse = RenderTextureFormat.ARGBFloat;
            else if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGB111110Float))
                m_RenderTextureFormatToUse = RenderTextureFormat.RGB111110Float;




            m_Render2DLightingPass = new Render2DLightingPass();

            m_Initialized = true;
        }

        public override void Setup(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            Init();

            RenderTextureDescriptor ambientRTDescriptor = m_AmbientRenderTextureInfo;
            RenderTextureDescriptor specularRTDescriptor = m_SpecularRenderTextureInfo;
            RenderTextureDescriptor rimRTDescriptor = m_RimRenderTextureInfo;

            m_Render2DLightingPass.Setup(m_AmbientDefaultColor, ambientRTDescriptor, specularRTDescriptor, rimRTDescriptor, m_AmbientRenderTextureInfo.filterMode, m_SpecularRenderTextureInfo.filterMode, m_RimRenderTextureInfo.filterMode);
            renderer.EnqueuePass(m_Render2DLightingPass);
        }


        #if UNITY_EDITOR
            [MenuItem("Assets/Create/Rendering/Create 2D Render Setup")]
            static void Create2DRenderSetup()
            {
                Default2DRendererSetup asset = ScriptableObject.CreateInstance<Default2DRendererSetup>();
                asset.name = "2D Render Setup";

                AssetDatabase.CreateAsset(asset, "Assets/NewScripableObject.asset");
                AssetDatabase.SaveAssets();

                EditorUtility.FocusProjectWindow();

                Selection.activeObject = asset;
            }
        #endif
    }
}

