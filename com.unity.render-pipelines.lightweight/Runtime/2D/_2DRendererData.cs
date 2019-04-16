using UnityEngine.Rendering;
using UnityEngine.Rendering.LWRP;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
#endif

namespace UnityEngine.Experimental.Rendering.LWRP
{
    public class _2DRendererData : ScriptableRendererData
    {
        [SerializeField]
        float m_HDREmulationScale = 1;

        [SerializeField]
        _2DLightOperationDescription[] m_LightOperations;

        [SerializeField]
        Shader m_ShapeLightShader = null;

        [SerializeField]
        Shader m_ShapeLightVolumeShader = null;

        [SerializeField]
        Shader m_PointLightShader = null;

        [SerializeField]
        Shader m_PointLightVolumeShader = null;

        [SerializeField]
        Shader m_BlitShader = null;

        public float hdrEmulationScale => m_HDREmulationScale;
        public _2DLightOperationDescription[] lightOperations => m_LightOperations;

        internal Shader shapeLightShader => m_ShapeLightShader;
        internal Shader shapeLightVolumeShader => m_ShapeLightVolumeShader;
        internal Shader pointLightShader => m_PointLightShader;
        internal Shader pointLightVolumeShader => m_PointLightVolumeShader;
        internal Shader blitShader => m_BlitShader;

        protected override ScriptableRenderer Create()
        {
            return new _2DRenderer(this);
        }

#if UNITY_EDITOR
        [MenuItem("Assets/Create/Rendering/Lightweight Render Pipeline/2D Renderer", priority = CoreUtils.assetCreateMenuPriority1 + 1)]
        static void Create2DRendererData()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<Create2DRendererDataAsset>(), "New 2D Renderer Data.asset", null, null);
        }

        class Create2DRendererDataAsset : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var instance = CreateInstance<_2DRendererData>();
                instance.OnCreate();
                AssetDatabase.CreateAsset(instance, pathName);
                Selection.activeObject = instance;
            }
        }

        void OnCreate()
        {
            m_LightOperations = new _2DLightOperationDescription[4];

            m_LightOperations[0].enabled = true;
            m_LightOperations[0].name = "Default";
            m_LightOperations[0].blendMode = _2DLightOperationDescription.BlendMode.Multiply;
            m_LightOperations[0].renderTextureScale = 1.0f;
            m_LightOperations[0].globalColor = new Color32(54, 58, 66, 255);    // This is the default ambient color in Lighting Settings. 

            for (int i = 1; i < m_LightOperations.Length; ++i)
            {
                m_LightOperations[i].enabled = false;
                m_LightOperations[i].name = "Unnamed " + i;
                m_LightOperations[i].blendMode = _2DLightOperationDescription.BlendMode.Multiply;
                m_LightOperations[i].renderTextureScale = 1.0f;
                m_LightOperations[i].globalColor = Color.black;
            }

            m_ShapeLightShader = Shader.Find("Hidden/Light2D-Shape");
            m_ShapeLightVolumeShader = Shader.Find("Hidden/Light2D-Shape-Volumetric");
            m_PointLightShader = Shader.Find("Hidden/Light2D-Point");
            m_PointLightVolumeShader = Shader.Find("Hidden/Light2d-Point-Volumetric");
            m_BlitShader = Shader.Find("Hidden/Lightweight Render Pipeline/Blit");
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            // Provide a list of suggested texture property names to Sprite Editor via EditorPrefs.
            const string suggestedNamesKey = "SecondarySpriteTexturePropertyNames";
            const string maskTex = "_MaskTex";
            const string normalMap = "_NormalMap";
            string suggestedNamesPrefs = EditorPrefs.GetString(suggestedNamesKey);

            if (string.IsNullOrEmpty(suggestedNamesPrefs))
                EditorPrefs.SetString(suggestedNamesKey, maskTex + "," + normalMap);
            else
            {
                if (!suggestedNamesPrefs.Contains(maskTex))
                    suggestedNamesPrefs += ("," + maskTex);

                if (!suggestedNamesPrefs.Contains(normalMap))
                    suggestedNamesPrefs += ("," + normalMap);

                EditorPrefs.SetString(suggestedNamesKey, suggestedNamesPrefs);
            }
        }

        internal override Material GetDefaultMaterial(DefaultMaterialType materialType)
        {
            if (materialType == DefaultMaterialType.Sprite || materialType == DefaultMaterialType.Particle)
                return AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.lightweight/Runtime/Materials/Sprite-Lit-Default.mat");

            return null;
        }

        internal override Shader GetDefaultShader()
        {
            return Shader.Find("Lightweight Render Pipeline/2D/Sprite-Lit-Default");
        }
#endif 
    }
}
