using UnityEngine.Rendering;
using UnityEngine.Rendering.LWRP;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    [CreateAssetMenu(fileName = "New 2D Renderer", menuName = "Rendering/Lightweight Render Pipeline/2D Renderer", order = CoreUtils.assetCreateMenuPriority1)]
    public class _2DRendererData : ScriptableRendererData
    {
        [SerializeField]
        float m_LightIntensityScale = 1;

        [SerializeField]
        [Serialization.FormerlySerializedAs("m_ShapeLightTypes")]
        _2DLightOperationDescription[] m_LightOperations;

        [SerializeField] Shader m_ShapeCookieSpriteAdditiveShader = null;
        [SerializeField] Shader m_ShapeCookieSpriteAlphaBlendShader = null;
        [SerializeField] Shader m_ShapeVertexColoredAdditiveShader = null;
        [SerializeField] Shader m_ShapeVertexColoredAlphaBlendShader = null;
        [SerializeField] Shader m_ShapeCookieSpriteVolumeShader = null;
        [SerializeField] Shader m_ShapeVertexColoredVolumeShader = null;
        [SerializeField] Shader m_PointLightShader = null;
        [SerializeField] Shader m_PointLightVolumeShader = null;

        public float lightIntensityScale => m_LightIntensityScale;
        public _2DLightOperationDescription[] lightOperations => m_LightOperations;

        internal Shader shapeCookieSpriteAdditiveShader => m_ShapeCookieSpriteAdditiveShader;
        internal Shader shapeCookieSpriteAlphaBlendShader => m_ShapeCookieSpriteAlphaBlendShader;
        internal Shader shapeVertexColoredAdditiveShader => m_ShapeVertexColoredAdditiveShader;
        internal Shader shapeVertexColoredAlphaBlendShader => m_ShapeVertexColoredAlphaBlendShader;
        internal Shader shapeCookieSpriteVolumeShader => m_ShapeCookieSpriteVolumeShader;
        internal Shader shapeVertexColoredVolumeShader => m_ShapeVertexColoredVolumeShader;
        internal Shader pointLightShader => m_PointLightShader;
        internal Shader pointLightVolumeShader => m_PointLightVolumeShader;

        public _2DRendererData()
        {
            m_LightOperations = new _2DLightOperationDescription[4];

            m_LightOperations[0].enabled = true;
            m_LightOperations[0].name = "Default";
            m_LightOperations[0].blendMode = _2DLightOperationDescription.BlendMode.Modulate;
            m_LightOperations[0].renderTextureScale = 1.0f;
            m_LightOperations[0].globalColor = new Color(0.2f,0.2f,0.2f,1.0f);

            for (int i = 1; i < m_LightOperations.Length; ++i)
            {
                m_LightOperations[i].enabled = false;
                m_LightOperations[i].name = "Disabled";
                m_LightOperations[i].blendMode = _2DLightOperationDescription.BlendMode.Modulate;
                m_LightOperations[i].renderTextureScale = 1.0f;
                m_LightOperations[i].globalColor = Color.black;
            }
        }

#if UNITY_EDITOR
        protected override void OnEnable()
        {
            base.OnEnable();

            m_ShapeCookieSpriteAdditiveShader = m_ShapeCookieSpriteAdditiveShader ?? Shader.Find("Hidden/Light2D-Sprite-Additive");
            m_ShapeCookieSpriteAlphaBlendShader = m_ShapeCookieSpriteAlphaBlendShader ?? Shader.Find("Hidden/Light2D-Sprite-Superimpose");
            m_ShapeVertexColoredAdditiveShader = m_ShapeVertexColoredAdditiveShader ?? Shader.Find("Hidden/Light2D-Shape-Additive");
            m_ShapeVertexColoredAlphaBlendShader = m_ShapeVertexColoredAlphaBlendShader ?? Shader.Find("Hidden/Light2D-Shape-Superimpose");
            m_ShapeCookieSpriteVolumeShader = m_ShapeCookieSpriteVolumeShader ?? Shader.Find("Hidden/Light2d-Sprite-Volumetric");
            m_ShapeVertexColoredVolumeShader = m_ShapeVertexColoredVolumeShader ?? Shader.Find("Hidden/Light2d-Shape-Volumetric");
            m_PointLightShader = m_PointLightShader ?? Shader.Find("Hidden/Light2D-Point");
            m_PointLightVolumeShader = m_PointLightVolumeShader ?? Shader.Find("Hidden/Light2d-Point-Volumetric");
        } 
#endif

        protected override ScriptableRenderer Create()
        {
            return new _2DRenderer(this);
        }
    }
}
