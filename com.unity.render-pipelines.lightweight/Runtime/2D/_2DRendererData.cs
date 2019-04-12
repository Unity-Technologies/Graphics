using UnityEngine.Rendering;
using UnityEngine.Rendering.LWRP;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    [CreateAssetMenu(fileName = "New 2D Renderer", menuName = "Rendering/Lightweight Render Pipeline/2D Renderer", order = CoreUtils.assetCreateMenuPriority1)]
    public class _2DRendererData : ScriptableRendererData
    {
        static Color defaultColor { get { return new Color(54.0f / 255.0f, 58.0f / 255.0f, 66.0f / 255.0f); } }  

        [SerializeField]
        float m_HDREmulationScale = 1;

        [SerializeField]
        _2DLightOperationDescription[] m_LightOperations;

        [SerializeField] Shader m_ShapeLightShader = null;
        [SerializeField] Shader m_ShapeLightVolumeShader = null;
        [SerializeField] Shader m_PointLightShader = null;
        [SerializeField] Shader m_PointLightVolumeShader = null;
        [SerializeField] Shader m_BlitShader = null;

        public float hdrEmulationScale => m_HDREmulationScale;
        public _2DLightOperationDescription[] lightOperations => m_LightOperations;

        internal Shader shapeLightShader => m_ShapeLightShader;
        internal Shader shapeLightVolumeShader => m_ShapeLightVolumeShader;
        internal Shader pointLightShader => m_PointLightShader;
        internal Shader pointLightVolumeShader => m_PointLightVolumeShader;
        internal Shader blitShader => m_BlitShader;

        public _2DRendererData()
        {
            m_LightOperations = new _2DLightOperationDescription[4];

            m_LightOperations[0].enabled = true;
            m_LightOperations[0].name = "Default";
            m_LightOperations[0].blendMode = _2DLightOperationDescription.BlendMode.Multiply;
            m_LightOperations[0].renderTextureScale = 1.0f;
            m_LightOperations[0].globalColor = defaultColor;

            for (int i = 1; i < m_LightOperations.Length; ++i)
            {
                m_LightOperations[i].enabled = false;
                m_LightOperations[i].name = "Disabled";
                m_LightOperations[i].blendMode = _2DLightOperationDescription.BlendMode.Multiply;
                m_LightOperations[i].renderTextureScale = 1.0f;
                m_LightOperations[i].globalColor = Color.black;
            }
        }

#if UNITY_EDITOR
        protected override void OnEnable()
        {
            base.OnEnable();

            m_ShapeLightShader = m_ShapeLightShader ?? Shader.Find("Hidden/Light2D-Shape");
            m_ShapeLightVolumeShader = m_ShapeLightVolumeShader ?? Shader.Find("Hidden/Light2D-Shape-Volumetric");
            m_PointLightShader = m_PointLightShader ?? Shader.Find("Hidden/Light2D-Point");
            m_PointLightVolumeShader = m_PointLightVolumeShader ?? Shader.Find("Hidden/Light2d-Point-Volumetric");
            m_BlitShader = m_BlitShader ?? Shader.Find("Hidden/Lightweight Render Pipeline/Blit");

            // Provide a list of suggested texture property names to Sprite Editor via EditorPrefs.
            const string suggestedNamesKey = "SecondarySpriteTexturePropertyNames";
            const string maskTex = "_MaskTex";
            const string normalMap = "_NormalMap";
            string suggestedNamesPrefs = UnityEditor.EditorPrefs.GetString(suggestedNamesKey);

            if (string.IsNullOrEmpty(suggestedNamesPrefs))
                UnityEditor.EditorPrefs.SetString(suggestedNamesKey, maskTex + "," + normalMap);
            else
            {
                if (!suggestedNamesPrefs.Contains(maskTex))
                    suggestedNamesPrefs += ("," + maskTex);

                if (!suggestedNamesPrefs.Contains(normalMap))
                    suggestedNamesPrefs += ("," + normalMap);

                UnityEditor.EditorPrefs.SetString(suggestedNamesKey, suggestedNamesPrefs);
            }
        }

        internal override Material GetDefaultMaterial(DefaultMaterialType materialType)
        {
            if (materialType == DefaultMaterialType.Sprite || materialType == DefaultMaterialType.Particle)
                return UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.lightweight/Runtime/Materials/Sprite-Lit-Default.mat");



            return null;
        }
#endif

        protected override ScriptableRenderer Create()
        {
            return new _2DRenderer(this);
        }
    }
}
