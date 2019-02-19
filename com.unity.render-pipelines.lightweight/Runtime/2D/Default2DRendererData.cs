using UnityEngine.Rendering.LWRP;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Experimental.Rendering.LWRP
{
    public class Default2DRendererData : ScriptableRendererData
    {
        [SerializeField]
        private float m_LightIntensityScale = 1;

        [SerializeField]
        [Serialization.FormerlySerializedAs("m_ShapeLightTypes")]
        private _2DLightOperationDescription[] m_LightOperations = new _2DLightOperationDescription[4];

        public _2DLightOperationDescription[] lightOperations
        {
            get => m_LightOperations;
        }

        public float lightIntensityScale
        {
            get => m_LightIntensityScale;
            set => m_LightIntensityScale = value;
        }

        public override ScriptableRenderer Create()
        {
            return new _2DRenderer(this);
        }

#if UNITY_EDITOR
        [MenuItem("Assets/Create/Rendering/2D Renderer Data")]
        static void CreateDefault2DRendererData()
        {
            Default2DRendererData asset = ScriptableObject.CreateInstance<Default2DRendererData>();
            asset.name = "2D Renderer Data";

            var lightOperation0 = new _2DLightOperationDescription();
            lightOperation0.enabled = true;
            lightOperation0.name = "Additive Light";
            lightOperation0.blendMode = _2DLightOperationDescription.BlendMode.Additive;
            lightOperation0.renderTextureScale = 1.0f;
            lightOperation0.globalColor = Color.black;
            asset.m_LightOperations[0] = lightOperation0;

            var lightOperation1 = new _2DLightOperationDescription();
            lightOperation1.enabled = true;
            lightOperation1.name = "Modulate Light";
            lightOperation1.blendMode = _2DLightOperationDescription.BlendMode.Modulate;
            lightOperation1.renderTextureScale = 1.0f;
            lightOperation1.globalColor = Color.gray;
            asset.m_LightOperations[1] = lightOperation1;

            AssetDatabase.CreateAsset(asset, "Assets/New 2D Renderer Data " + Random.Range(0, 100000) + ".asset");
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
        }
#endif
    }
}
