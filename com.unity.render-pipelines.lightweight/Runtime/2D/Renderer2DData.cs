using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.LWRP;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
#endif


namespace UnityEngine.Experimental.Rendering.LWRP
{
    public class Renderer2DData : ScriptableRendererData
    {
        [SerializeField]
        float m_HDREmulationScale = 1;

        [SerializeField, FormerlySerializedAs("m_LightOperations")]
        Light2DBlendStyle[] m_LightBlendStyles = null;

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
        public Light2DBlendStyle[] lightBlendStyles => m_LightBlendStyles;

        internal Shader shapeLightShader => m_ShapeLightShader;
        internal Shader shapeLightVolumeShader => m_ShapeLightVolumeShader;
        internal Shader pointLightShader => m_PointLightShader;
        internal Shader pointLightVolumeShader => m_PointLightVolumeShader;
        internal Shader blitShader => m_BlitShader;

        protected override ScriptableRenderer Create()
        {
            return new Renderer2D(this);
        }

#if UNITY_EDITOR
        internal static void Create2DRendererData(Action<Renderer2DData> onCreatedCallback)
        {
            var instance = CreateInstance<Create2DRendererDataAsset>();
            instance.onCreated += onCreatedCallback;
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, instance, "New 2D Renderer Data.asset", null, null);
        }

        class Create2DRendererDataAsset : EndNameEditAction
        {
            public event Action<Renderer2DData> onCreated;

            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var instance = CreateInstance<Renderer2DData>();
                instance.OnCreate();
                AssetDatabase.CreateAsset(instance, pathName);
                Selection.activeObject = instance;

                onCreated(instance);
            }
        }

        void OnCreate()
        {
            m_LightBlendStyles = new Light2DBlendStyle[4];

            m_LightBlendStyles[0].enabled = true;
            m_LightBlendStyles[0].name = "Default";
            m_LightBlendStyles[0].blendMode = Light2DBlendStyle.BlendMode.Multiply;
            m_LightBlendStyles[0].renderTextureScale = 1.0f;

            for (int i = 1; i < m_LightBlendStyles.Length; ++i)
            {
                m_LightBlendStyles[i].enabled = false;
                m_LightBlendStyles[i].name = "Blend Style " + i;
                m_LightBlendStyles[i].blendMode = Light2DBlendStyle.BlendMode.Multiply;
                m_LightBlendStyles[i].renderTextureScale = 1.0f;
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
