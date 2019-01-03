using UnityEngine.Experimental.Rendering.LWRP;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class Default2DRendererData : IRendererData
    {
        [SerializeField]
        internal Light2DRTInfo m_AmbientRenderTextureInfo = new Light2DRTInfo(true, 64, 64, FilterMode.Bilinear);
        [SerializeField]
        internal Light2DRTInfo m_SpecularRenderTextureInfo = new Light2DRTInfo(true, 1024, 512, FilterMode.Bilinear);
        [SerializeField]
        internal Light2DRTInfo m_RimRenderTextureInfo = new Light2DRTInfo(false, 64, 64, FilterMode.Bilinear);
        //[SerializeField]
        //internal Light2DRTInfo m_ShadowRenderTextureInfo = new Light2DRTInfo(true, 1024, 512, FilterMode.Bilinear);
        [SerializeField]
        internal Light2DRTInfo m_PointLightNormalRenderTextureInfo = new Light2DRTInfo(false, 512, 512, FilterMode.Bilinear);
        [SerializeField]
        internal Light2DRTInfo m_PointLightColorRenderTextureInfo = new Light2DRTInfo(false, 512, 512, FilterMode.Bilinear);
        [SerializeField]
        private float m_LightIntensityScale = 1;

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

        public override IRendererSetup Create()
        {
            return new Default2DRendererSetup(this);
        }

#if UNITY_EDITOR
        [MenuItem("Assets/Create/Rendering/2D Renderer Data")]
        static void CreateDefault2DRendererData()
        {
            Default2DRendererData asset = ScriptableObject.CreateInstance<Default2DRendererData>();
            asset.name = "2D Renderer Data";

            AssetDatabase.CreateAsset(asset, "Assets/New 2D Renderer Data.asset");
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
        }
#endif
    }
}
