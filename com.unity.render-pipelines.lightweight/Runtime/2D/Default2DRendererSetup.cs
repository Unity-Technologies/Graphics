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
        [SerializeField]
        private Light2DRTInfo m_AmbientRenderTextureInfo = new Light2DRTInfo(true, 64, 64, FilterMode.Bilinear);
        [SerializeField]
        private Light2DRTInfo m_SpecularRenderTextureInfo = new Light2DRTInfo(true, 1024, 512, FilterMode.Bilinear);
        [SerializeField]
        private Light2DRTInfo m_RimRenderTextureInfo = new Light2DRTInfo(false, 64, 64, FilterMode.Bilinear);
        //[SerializeField]
        //private Light2DRTInfo m_ShadowRenderTextureInfo = new Light2DRTInfo(true, 1024, 512, FilterMode.Bilinear);
        [SerializeField]
        private Light2DRTInfo m_PointLightNormalRenderTextureInfo = new Light2DRTInfo(false, 512, 512, FilterMode.Bilinear);
        [SerializeField]
        private Light2DRTInfo m_PointLightColorRenderTextureInfo = new Light2DRTInfo(false, 512, 512, FilterMode.Bilinear);
        [SerializeField]
        private float         m_LightIntensityScale = 1;


        private DrawSkyboxPass m_DrawSkyboxPass;

        private Render2DLightingPass m_Render2DLightingPass;
        private SetupForwardRenderingPass m_SetupForwardRenderingPass;


        private RenderTargetHandle m_DepthTexture;
#if UNITY_EDITOR
        private SceneViewDepthCopyPass m_SceneViewDepthCopyPass;
#endif

        public float LightIntensityScale
        {
            set
            {
                m_LightIntensityScale = value;
            }

            get
            {
                return m_LightIntensityScale;
            }
        }



        //Render2DFallbackPass   m_Render2DFallbackPass;

        [NonSerialized]
        private bool m_Initialized = false;

        private void Init()
        {
            if (m_Initialized)
                return;

            m_DepthTexture.Init("_CameraDepthTexture");
            m_Render2DLightingPass = new Render2DLightingPass();
            m_SetupForwardRenderingPass = new SetupForwardRenderingPass();

            m_DrawSkyboxPass = new DrawSkyboxPass();


#if UNITY_EDITOR
            m_SceneViewDepthCopyPass = new SceneViewDepthCopyPass();
#endif
            m_Initialized = true;
        }

        public override void Setup(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            Init();

            renderer.EnqueuePass(m_SetupForwardRenderingPass);

            m_Render2DLightingPass.Setup(RenderSettings.ambientLight, m_AmbientRenderTextureInfo, m_SpecularRenderTextureInfo, m_RimRenderTextureInfo, m_PointLightNormalRenderTextureInfo, m_PointLightColorRenderTextureInfo, m_LightIntensityScale);
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

