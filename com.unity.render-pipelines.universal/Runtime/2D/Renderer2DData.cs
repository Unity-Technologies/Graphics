using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
#endif

namespace UnityEngine.Experimental.Rendering.Universal
{
    [Serializable, ReloadGroup, ExcludeFromPreset]
    [MovedFrom("UnityEngine.Experimental.Rendering.LWRP")]
    public class Renderer2DData : ScriptableRendererData
    {
        public enum Renderer2DDefaultMaterialType
        {
            Lit,
            Unlit,
            Custom
        }

        [SerializeField]
        TransparencySortMode m_TransparencySortMode = TransparencySortMode.Default;

        [SerializeField]
        Vector3 m_TransparencySortAxis = Vector3.up;

        [SerializeField]
        float m_HDREmulationScale = 1;

        [SerializeField, FormerlySerializedAs("m_LightOperations")]
        Light2DBlendStyle[] m_LightBlendStyles = null;

        [SerializeField]
        bool m_UseDepthStencilBuffer = true;

#if UNITY_EDITOR
        [SerializeField]
        Renderer2DDefaultMaterialType m_DefaultMaterialType = Renderer2DDefaultMaterialType.Lit;

        [SerializeField, Reload("Runtime/Materials/Sprite-Lit-Default.mat")]
        Material m_DefaultCustomMaterial = null;

        [SerializeField, Reload("Runtime/Materials/Sprite-Lit-Default.mat")]
        Material m_DefaultLitMaterial = null;

        [SerializeField, Reload("Runtime/Materials/Sprite-Unlit-Default.mat")]
        Material m_DefaultUnlitMaterial = null;
#endif

        [SerializeField, Reload("Shaders/2D/Light2D-Shape.shader")]
        Shader m_ShapeLightShader = null;

        [SerializeField, Reload("Shaders/2D/Light2D-Shape-Volumetric.shader")]
        Shader m_ShapeLightVolumeShader = null;

        [SerializeField, Reload("Shaders/2D/Light2D-Point.shader")]
        Shader m_PointLightShader = null;

        [SerializeField, Reload("Shaders/2D/Light2D-Point-Volumetric.shader")]
        Shader m_PointLightVolumeShader = null;

        [SerializeField, Reload("Shaders/Utils/Blit.shader")]
        Shader m_BlitShader = null;

        [SerializeField, Reload("Shaders/2D/ShadowGroup2D.shader")]
        Shader m_ShadowGroupShader = null;

        [SerializeField, Reload("Shaders/2D/Shadow2DRemoveSelf.shader")]
        Shader m_RemoveSelfShadowShader = null;

        [SerializeField, Reload("Runtime/Data/PostProcessData.asset")]
        PostProcessData m_PostProcessData = null;

        public float hdrEmulationScale => m_HDREmulationScale;
        public Light2DBlendStyle[] lightBlendStyles => m_LightBlendStyles;
        internal bool useDepthStencilBuffer => m_UseDepthStencilBuffer;

        internal Shader shapeLightShader => m_ShapeLightShader;
        internal Shader shapeLightVolumeShader => m_ShapeLightVolumeShader;
        internal Shader pointLightShader => m_PointLightShader;
        internal Shader pointLightVolumeShader => m_PointLightVolumeShader;
        internal Shader blitShader => m_BlitShader;
        internal Shader shadowGroupShader => m_ShadowGroupShader;
        internal Shader removeSelfShadowShader => m_RemoveSelfShadowShader;
        internal PostProcessData postProcessData => m_PostProcessData;
        internal TransparencySortMode transparencySortMode => m_TransparencySortMode;
        internal Vector3 transparencySortAxis => m_TransparencySortAxis;

        protected override ScriptableRenderer Create()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                ResourceReloader.TryReloadAllNullIn(this, UniversalRenderPipelineAsset.packagePath);
                ResourceReloader.TryReloadAllNullIn(m_PostProcessData, UniversalRenderPipelineAsset.packagePath);
            }
#endif
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

            m_LightBlendStyles[0].name = "Default";
            m_LightBlendStyles[0].blendMode = Light2DBlendStyle.BlendMode.Multiply;
            m_LightBlendStyles[0].renderTextureScale = 1.0f;

            for (int i = 1; i < m_LightBlendStyles.Length; ++i)
            {
                m_LightBlendStyles[i].name = "Blend Style " + i;
                m_LightBlendStyles[i].blendMode = Light2DBlendStyle.BlendMode.Multiply;
                m_LightBlendStyles[i].renderTextureScale = 1.0f;
            }
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

            ResourceReloader.TryReloadAllNullIn(this, UniversalRenderPipelineAsset.packagePath);
            ResourceReloader.TryReloadAllNullIn(m_PostProcessData, UniversalRenderPipelineAsset.packagePath);
        }

        internal override Material GetDefaultMaterial(DefaultMaterialType materialType)
        {
            if (materialType == DefaultMaterialType.Sprite || materialType == DefaultMaterialType.Particle)
            {
                if (m_DefaultMaterialType == Renderer2DDefaultMaterialType.Lit)
                    return m_DefaultLitMaterial;
                else if (m_DefaultMaterialType == Renderer2DDefaultMaterialType.Unlit)
                    return m_DefaultUnlitMaterial;
                else
                    return m_DefaultCustomMaterial;
            }

            return null;
        }


        internal override Shader GetDefaultShader()
        {
            return Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
        }
#endif
    }
}
