using UnityEditor;

namespace UnityEngine.Rendering.Universal
{
    public partial class Renderer2DData
    {
#if UNITY_EDITOR
        [SerializeField]
        Renderer2DDefaultMaterialType m_DefaultMaterialType = Renderer2DDefaultMaterialType.Lit;

        [SerializeField, Reload("Runtime/Materials/Sprite-Lit-Default.mat")]
        Material m_DefaultCustomMaterial = null;

        internal override Shader GetDefaultShader()
        {
            if (!GraphicsSettings.TryGetRenderPipelineSettings<Renderer2DResources>(out var resources))
                return null;

            return resources.defaultLitMaterial.shader;
        }

        internal override Material GetDefaultMaterial(DefaultMaterialType materialType)
        {
            if (!GraphicsSettings.TryGetRenderPipelineSettings<Renderer2DResources>(out var resources))
                return null;

            switch (materialType)
            {
                case DefaultMaterialType.Sprite:
                case DefaultMaterialType.Particle:
                {
                    return m_DefaultMaterialType switch
                    {
                        Renderer2DDefaultMaterialType.Lit => resources.defaultLitMaterial,
                        Renderer2DDefaultMaterialType.Unlit => resources.defaultUnlitMaterial,
                        _ => m_DefaultCustomMaterial
                    };
                }
                case DefaultMaterialType.SpriteMask:
                    return resources.defaultMaskMaterial;
                default:
                    return null;
            }
        }

        private void InitializeSpriteEditorPrefs()
        {
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

            ReloadAllNullProperties();
        }

        private void ReloadAllNullProperties()
        {
            ResourceReloader.TryReloadAllNullIn(this, UniversalRenderPipelineAsset.packagePath);
        }

        void RebuildBlendStyles(bool force = false)
        {
            // Initialize Editor Prefs for Sprite Editor
            InitializeSpriteEditorPrefs();

            // Initialize Light Blend Styles
            if (m_LightBlendStyles != null && !force)
            {
                for (int i = 0; i < m_LightBlendStyles.Length; ++i)
                {
                    ref var blendStyle = ref m_LightBlendStyles[i];

                    // Custom blend mode (99) now falls back to Multiply.
                    if ((int) blendStyle.blendMode == 99)
                        blendStyle.blendMode = Light2DBlendStyle.BlendMode.Multiply;
                }

                return;
            }

            m_LightBlendStyles = new Light2DBlendStyle[4];

            m_LightBlendStyles[0].name = "Multiply";
            m_LightBlendStyles[0].blendMode = Light2DBlendStyle.BlendMode.Multiply;

            m_LightBlendStyles[1].name = "Additive";
            m_LightBlendStyles[1].blendMode = Light2DBlendStyle.BlendMode.Additive;

            m_LightBlendStyles[2].name = "Multiply with Mask";
            m_LightBlendStyles[2].blendMode = Light2DBlendStyle.BlendMode.Multiply;
            m_LightBlendStyles[2].maskTextureChannel = Light2DBlendStyle.TextureChannel.R;

            m_LightBlendStyles[3].name = "Additive with Mask";
            m_LightBlendStyles[3].blendMode = Light2DBlendStyle.BlendMode.Additive;
            m_LightBlendStyles[3].maskTextureChannel = Light2DBlendStyle.TextureChannel.R;
        }

        private void Awake()
        {
            RebuildBlendStyles();
        }

        void Reset()
        {
            RebuildBlendStyles(true);
        }
#endif
    }
}
