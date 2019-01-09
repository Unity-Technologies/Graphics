#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Experimental.Rendering.LWRP
{
    public class Default2DRendererData : IRendererData
    {
        [SerializeField]
        internal Light2DRTInfo m_PointLightNormalRenderTextureInfo = new Light2DRTInfo(false, 512, 512, FilterMode.Bilinear);

        [SerializeField]
        internal Light2DRTInfo m_PointLightColorRenderTextureInfo = new Light2DRTInfo(false, 512, 512, FilterMode.Bilinear);

        [SerializeField]
        private float m_LightIntensityScale = 1;

        [SerializeField]
        private _2DShapeLightTypeDescription[] m_ShapeLightTypes = new _2DShapeLightTypeDescription[3];

        public _2DShapeLightTypeDescription[] shapeLightTypes
        {
            get => m_ShapeLightTypes;
        }

        public float LightIntensityScale
        {
            get => m_LightIntensityScale;
            set => m_LightIntensityScale = value;
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

            var shapeLightType0 = new _2DShapeLightTypeDescription();
            shapeLightType0.enabled = true;
            shapeLightType0.name = "Additive Light";
            shapeLightType0.blendMode = _2DShapeLightTypeDescription.BlendMode.Additive;
            shapeLightType0.renderTextureScale = 1.0f;
            shapeLightType0.globalColor = Color.black;
            asset.m_ShapeLightTypes[0] = shapeLightType0;

            var shapeLightType1 = new _2DShapeLightTypeDescription();
            shapeLightType1.enabled = true;
            shapeLightType1.name = "Modulate Light";
            shapeLightType1.blendMode = _2DShapeLightTypeDescription.BlendMode.Modulate;
            shapeLightType1.renderTextureScale = 1.0f;
            shapeLightType1.globalColor = Color.gray;
            asset.m_ShapeLightTypes[1] = shapeLightType1;

            AssetDatabase.CreateAsset(asset, "Assets/New 2D Renderer Data " + Random.Range(0, 100000) + ".asset");
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
        }
#endif
    }
}
