using UnityEditor;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    [CustomEditor(typeof(LightweightPipelineAsset))]
    public class LightweightAssetInspector : Editor
    {
        internal class Styles
        {
            public static GUIContent renderingLabel = new GUIContent("Rendering");
            public static GUIContent shadowLabel = new GUIContent("Shadows");
            public static GUIContent defaults = new GUIContent("Defaults");
            public static GUIContent linearRenderingLabel = new GUIContent("Force Linear Colorspace", "When enabled Lightweight shader will perform gamma to linear conversion in the shader when linear rendering is not supported or disabled");

            public static GUIContent renderScaleLabel = new GUIContent("Render Scale", "Allows game to render at a resolution different than native resolution. UI is always rendered at native resolution.");

            public static GUIContent maxPixelLights = new GUIContent("Per-Object Pixel Lights",
                    "Max amount of dynamic per-object pixel lights.");

            public static GUIContent enableVertexLightLabel = new GUIContent("Enable Vertex Light",
                    "Lightweight pipeline support at most 4 per-object lights between pixel and vertex. If value in pixel lights is set to max this settings has no effect.");

            public static GUIContent shadowType = new GUIContent("Shadow Type",
                    "Single directional shadow supported. SOFT_SHADOWS applies shadow filtering.");

            public static GUIContent shadowNearPlaneOffset = new GUIContent("Shadow Near Plane Offset",
                    "Offset shadow near plane to account for large triangles being distorted by pancaking");

            public static GUIContent shadowDistante = new GUIContent("Shadow Distance", "Max shadow drawing distance");

            public static GUIContent shadowAtlasResolution = new GUIContent("Shadow Map Resolution",
                    "Resolution of shadow map texture. If cascades are enabled all cascades will be packed into this texture resolution.");

            public static GUIContent shadowCascades = new GUIContent("Shadow Cascades",
                    "Number of cascades for directional shadows");

            public static GUIContent shadowCascadeSplit = new GUIContent("Shadow Cascade Split",
                "Percentages to split shadow volume");

            public static GUIContent defaultDiffuseMaterial = new GUIContent("Default Diffuse Material",
                "Material to use when creating 3D objects");

            public static GUIContent defaultParticleMaterial = new GUIContent("Default Particle Material",
                "Material to use when creating Paticle Systems");

            public static GUIContent defaultLineMaterial = new GUIContent("Default Line Material",
                "Material to use when creating Line Renderers");

            public static GUIContent defaultSpriteMaterial = new GUIContent("Default Sprite Material",
                "Material to use when creating Sprites");

            public static GUIContent defaultUIMaterial = new GUIContent("Default UI Material", "Material to use when creating UI Text");

            public static GUIContent defaultShader = new GUIContent("Default Shader",
                "Shader to use when creating materials");

            public static GUIContent msaaContent = new GUIContent("Anti Aliasing (MSAA)", "Controls the global anti aliasing quality. When set to disabled, MSAA will not be performed even if the camera allows it.");

            public static GUIContent attenuationTextureLabel = new GUIContent("Attenuation Texture", "Light attenuation falloff texture");
        }

        private int kMaxSupportedPixelLights = 5;
        private SerializedProperty m_LinearRenderingProperty;
        private SerializedProperty m_RenderScale;
        private SerializedProperty m_MaxPixelLights;
        private SerializedProperty m_SupportsVertexLightProp;
        private SerializedProperty m_ShadowTypeProp;
        private SerializedProperty m_ShadowNearPlaneOffsetProp;
        private SerializedProperty m_ShadowDistanceProp;
        private SerializedProperty m_ShadowAtlasResolutionProp;
        private SerializedProperty m_ShadowCascadesProp;
        private SerializedProperty m_ShadowCascade2SplitProp;
        private SerializedProperty m_ShadowCascade4SplitProp;
        private SerializedProperty m_DefaultDiffuseMaterial;
        private SerializedProperty m_DefaultParticleMaterial;
        private SerializedProperty m_DefaultLineMaterial;
        private SerializedProperty m_DefaultSpriteMaterial;
        private SerializedProperty m_DefaultUIMaterial;
        private SerializedProperty m_DefaultShader;
        private SerializedProperty m_MSAA;
        private SerializedProperty m_AttenuationTexture;

        void OnEnable()
        {
            m_LinearRenderingProperty = serializedObject.FindProperty("m_LinearRendering");
            m_RenderScale = serializedObject.FindProperty("m_RenderScale");
            m_MaxPixelLights = serializedObject.FindProperty("m_MaxPixelLights");
            m_SupportsVertexLightProp = serializedObject.FindProperty("m_SupportsVertexLight");
            m_ShadowTypeProp = serializedObject.FindProperty("m_ShadowType");
            m_ShadowNearPlaneOffsetProp = serializedObject.FindProperty("m_ShadowNearPlaneOffset");
            m_ShadowDistanceProp = serializedObject.FindProperty("m_ShadowDistance");
            m_ShadowAtlasResolutionProp = serializedObject.FindProperty("m_ShadowAtlasResolution");
            m_ShadowCascadesProp = serializedObject.FindProperty("m_ShadowCascades");
            m_ShadowCascade2SplitProp = serializedObject.FindProperty("m_Cascade2Split");
            m_ShadowCascade4SplitProp = serializedObject.FindProperty("m_Cascade4Split");
            m_DefaultDiffuseMaterial = serializedObject.FindProperty("m_DefaultDiffuseMaterial");
            m_DefaultParticleMaterial = serializedObject.FindProperty("m_DefaultParticleMaterial");
            m_DefaultLineMaterial = serializedObject.FindProperty("m_DefaultLineMaterial");
            m_DefaultSpriteMaterial = serializedObject.FindProperty("m_DefaultSpriteMaterial");
            m_DefaultUIMaterial = serializedObject.FindProperty("m_DefaultUIMaterial");
            m_DefaultShader = serializedObject.FindProperty("m_DefaultShader");
            m_MSAA = serializedObject.FindProperty("m_MSAA");
            m_AttenuationTexture = serializedObject.FindProperty("m_AttenuationTexture");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Styles.renderingLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_LinearRenderingProperty, Styles.linearRenderingLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(Styles.renderScaleLabel);
            m_RenderScale.floatValue = EditorGUILayout.Slider(m_RenderScale.floatValue, 0.1f, 1.0f);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(Styles.maxPixelLights);
            m_MaxPixelLights.intValue = EditorGUILayout.IntSlider(m_MaxPixelLights.intValue, 0, kMaxSupportedPixelLights);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(m_SupportsVertexLightProp, Styles.enableVertexLightLabel);
            EditorGUILayout.PropertyField(m_MSAA, Styles.msaaContent);
            EditorGUILayout.PropertyField(m_AttenuationTexture, Styles.attenuationTextureLabel);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField(Styles.shadowLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_ShadowTypeProp, Styles.shadowType);
            EditorGUILayout.PropertyField(m_ShadowAtlasResolutionProp, Styles.shadowAtlasResolution);
            EditorGUILayout.PropertyField(m_ShadowNearPlaneOffsetProp, Styles.shadowNearPlaneOffset);
            EditorGUILayout.PropertyField(m_ShadowDistanceProp, Styles.shadowDistante);
            EditorGUILayout.PropertyField(m_ShadowCascadesProp, Styles.shadowCascades);

            ShadowCascades cascades = (ShadowCascades)m_ShadowCascadesProp.intValue;
            if (cascades == ShadowCascades.FOUR_CASCADES)
            {
                EditorGUILayout.PropertyField(m_ShadowCascade4SplitProp, Styles.shadowCascadeSplit);
            }
            else if (cascades == ShadowCascades.TWO_CASCADES)
            {
                EditorGUILayout.PropertyField(m_ShadowCascade2SplitProp, Styles.shadowCascadeSplit);
            }

            EditorGUI.indentLevel--;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Styles.defaults, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_DefaultDiffuseMaterial, Styles.defaultDiffuseMaterial);
            EditorGUILayout.PropertyField(m_DefaultParticleMaterial, Styles.defaultParticleMaterial);
            EditorGUILayout.PropertyField(m_DefaultLineMaterial, Styles.defaultLineMaterial);
            EditorGUILayout.PropertyField(m_DefaultSpriteMaterial, Styles.defaultSpriteMaterial);
            EditorGUILayout.PropertyField(m_DefaultUIMaterial, Styles.defaultUIMaterial);
            EditorGUILayout.PropertyField(m_DefaultShader, Styles.defaultShader);
            EditorGUI.indentLevel--;

            serializedObject.ApplyModifiedProperties();
        }
    }
}
