using UnityEngine.Rendering.LWRP;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    [CreateAssetMenu(fileName = "New 2D Renderer", menuName = "Rendering/Lightweight Render Pipeline/2D Renderer", order = 2)]
    public class _2DRendererData : ScriptableRendererData
    {
        [SerializeField]
        float m_LightIntensityScale = 1;

        [SerializeField]
        [Serialization.FormerlySerializedAs("m_ShapeLightTypes")]
        _2DLightOperationDescription[] m_LightOperations;

        public float lightIntensityScale
        {
            get => m_LightIntensityScale;
            set => m_LightIntensityScale = value;
        }

        public _2DLightOperationDescription[] lightOperations
        {
            get => m_LightOperations;
        }

        public _2DRendererData()
        {
            m_LightOperations = new _2DLightOperationDescription[4];

            m_LightOperations[0].enabled = true;
            m_LightOperations[0].name = "Additive Light";
            m_LightOperations[0].blendMode = _2DLightOperationDescription.BlendMode.Additive;
            m_LightOperations[0].renderTextureScale = 1.0f;
            m_LightOperations[0].globalColor = Color.black;

            m_LightOperations[1].enabled = true;
            m_LightOperations[1].name = "Modulate Light";
            m_LightOperations[1].blendMode = _2DLightOperationDescription.BlendMode.Modulate;
            m_LightOperations[1].renderTextureScale = 1.0f;
            m_LightOperations[1].globalColor = Color.gray;

            for (int i = 2; i < m_LightOperations.Length; ++i)
            {
                m_LightOperations[i].enabled = false;
                m_LightOperations[i].name = "Disabled";
                m_LightOperations[i].blendMode = _2DLightOperationDescription.BlendMode.Additive;
                m_LightOperations[i].renderTextureScale = 1.0f;
                m_LightOperations[i].globalColor = Color.black;
            }
        }

        protected override ScriptableRenderer Create()
        {
            return new _2DRenderer(this);
        }
    }
}
