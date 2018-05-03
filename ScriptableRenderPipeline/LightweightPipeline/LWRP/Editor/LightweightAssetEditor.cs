using System.Linq;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditor.Experimental.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    [CustomEditor(typeof(LightweightPipelineAsset))]
    public class LightweightAssetEditor : Editor
    {
        internal class Styles
        {
            public static GUIContent renderingLabel = new GUIContent("Rendering");
            public static GUIContent shadowLabel = new GUIContent("Shadows");

            public static GUIContent renderScaleLabel = new GUIContent("Render Scale", "Scales the camera render target allowing the game to render at a resolution different than native resolution. UI is always rendered at native resolution. When in VR mode, VR scaling configuration is used instead.");

            public static GUIContent maxPixelLightsLabel = new GUIContent("Pixel Lights",
                    "Controls the amount of pixel lights that run in fragment light loop. Lights are sorted and culled per-object.");

            public static GUIContent enableVertexLightLabel = new GUIContent("Vertex Lighting",
                    "If enabled shades additional lights exceeding the maximum number of pixel lights per-vertex up to the maximum of 8 lights.");

            public static GUIContent requireDepthTexture = new GUIContent("Depth Texture", "If enabled the pipeline will generate camera's depth that can be bound in shaders as _CameraDepthTexture.");

            public static GUIContent requireSoftParticles = new GUIContent("Soft Particles", "If enabled the pipeline will enable SOFT_PARTICLES keyword.");

            public static GUIContent requireOpaqueTexture = new GUIContent("Opaque Texture", "If enabled the pipeline will copy the screen to texture after opaque objects are drawn. For transparent objects this can be bound in shaders as _CameraOpaqueTexture.");

            public static GUIContent opaqueDownsampling = new GUIContent("Opaque Downsampling", "The downsampling method that is used for the opaque texture");
            public static GUIContent hdrContent = new GUIContent("HDR", "Controls the global HDR settings.");
            public static GUIContent msaaContent = new GUIContent("Anti Aliasing (MSAA)", "Controls the global anti aliasing settings.");


            public static GUIContent supportsSoftShadows = new GUIContent("Soft Shadows", "If enabled pipeline will perform shadow filtering. Otherwise all lights that cast shadows will fallback to perform a single shadow sample.");
            public static GUIContent supportsDirectionalShadows = new GUIContent("Directional Shadows", "If enabled shadows will be supported for directional lights.");

            public static GUIContent shadowDistance = new GUIContent("Distance", "Max shadow rendering distance.");

            public static GUIContent directionalShadowAtlasResolution = new GUIContent("Atlas Resolution",
                    "Resolution of the directional shadow map texture. If cascades are enabled, cascades will be packed into an atlas and this setting controls the max shadows atlas resolution.");

            public static GUIContent shadowCascades = new GUIContent("Cascades",
                    "Number of cascades used in directional lights shadows");

            public static GUIContent shadowCascadeSplit = new GUIContent("Cascades Split",
                    "Percentages to split shadow volume");

            public static GUIContent supportsLocalShadows = new GUIContent("Local Shadows", "If enabled shadows will be supported for spot lights.");

            public static GUIContent localShadowsAtlasResolution = new GUIContent("Atlas Resolution",
                    "All local lights are packed into a single atlas. This setting controls the atlas size.");

            public static string[] shadowCascadeOptions = {"No Cascades", "Two Cascades", "Four Cascades"};
            public static string[] opaqueDownsamplingOptions = {"None", "2x (Bilinear)", "4x (Box)", "4x (Bilinear)"};
        }

        AnimBool m_ShowSoftParticles = new AnimBool();
        AnimBool m_ShowOpaqueTextureScale = new AnimBool();


        private int kMaxSupportedPixelLights = 8;
        private float kMinRenderScale = 0.1f;
        private float kMaxRenderScale = 4.0f;
        private SerializedProperty m_RenderScale;
        private SerializedProperty m_MaxPixelLights;
        private SerializedProperty m_SupportsVertexLightProp;
        private SerializedProperty m_RequireDepthTextureProp;
        private SerializedProperty m_RequireSoftParticlesProp;
        private SerializedProperty m_RequireOpaqueTextureProp;
        private SerializedProperty m_OpaqueDownsamplingProp;
        private SerializedProperty m_HDR;
        private SerializedProperty m_MSAA;

        private SerializedProperty m_SoftShadowsSupportedProp;
        private SerializedProperty m_DirectionalShadowsSupportedProp;
        private SerializedProperty m_ShadowDistanceProp;
        private SerializedProperty m_DirectionalShadowAtlasResolutionProp;
        private SerializedProperty m_ShadowCascadesProp;
        private SerializedProperty m_ShadowCascade2SplitProp;
        private SerializedProperty m_ShadowCascade4SplitProp;
        private SerializedProperty m_LocalShadowSupportedProp;
        private SerializedProperty m_LocalShadowsAtlasResolutionProp;

        void OnEnable()
        {
            m_RenderScale = serializedObject.FindProperty("m_RenderScale");
            m_MaxPixelLights = serializedObject.FindProperty("m_MaxPixelLights");
            m_SupportsVertexLightProp = serializedObject.FindProperty("m_SupportsVertexLight");
            m_RequireDepthTextureProp = serializedObject.FindProperty("m_RequireDepthTexture");
            m_RequireSoftParticlesProp = serializedObject.FindProperty("m_RequireSoftParticles");
            m_RequireOpaqueTextureProp = serializedObject.FindProperty("m_RequireOpaqueTexture");
            m_OpaqueDownsamplingProp = serializedObject.FindProperty("m_OpaqueDownsampling");
            m_HDR = serializedObject.FindProperty("m_SupportsHDR");
            m_MSAA = serializedObject.FindProperty("m_MSAA");

            m_DirectionalShadowsSupportedProp = serializedObject.FindProperty("m_DirectionalShadowsSupported");
            m_ShadowDistanceProp = serializedObject.FindProperty("m_ShadowDistance");
            m_DirectionalShadowAtlasResolutionProp = serializedObject.FindProperty("m_ShadowAtlasResolution");
            m_ShadowCascadesProp = serializedObject.FindProperty("m_ShadowCascades");
            m_ShadowCascade2SplitProp = serializedObject.FindProperty("m_Cascade2Split");
            m_ShadowCascade4SplitProp = serializedObject.FindProperty("m_Cascade4Split");
            m_LocalShadowSupportedProp = serializedObject.FindProperty("m_LocalShadowsSupported");
            m_LocalShadowsAtlasResolutionProp = serializedObject.FindProperty("m_LocalShadowsAtlasResolution");
            m_SoftShadowsSupportedProp = serializedObject.FindProperty("m_SoftShadowsSupported");

            m_ShowSoftParticles.valueChanged.AddListener(Repaint);
            m_ShowSoftParticles.value = m_RequireSoftParticlesProp.boolValue;
            m_ShowOpaqueTextureScale.valueChanged.AddListener(Repaint);
            m_ShowOpaqueTextureScale.value = m_RequireOpaqueTextureProp.boolValue;
        }

        void OnDisable()
        {
            m_ShowSoftParticles.valueChanged.RemoveListener(Repaint);
            m_ShowOpaqueTextureScale.valueChanged.RemoveListener(Repaint);
        }

        void UpdateAnimationValues()
        {
            m_ShowSoftParticles.target = m_RequireDepthTextureProp.boolValue;
            m_ShowOpaqueTextureScale.target = m_RequireOpaqueTextureProp.boolValue;
        }

        void DrawAnimatedProperty(SerializedProperty prop, GUIContent content, AnimBool animation)
        {
            using (var group = new EditorGUILayout.FadeGroupScope(animation.faded))
                if (group.visible)
                    EditorGUILayout.PropertyField(prop, content);
        }

        void DrawAnimatedPopup(SerializedProperty prop, GUIContent content, string[] options, AnimBool animation)
        {
            using (var group = new EditorGUILayout.FadeGroupScope(animation.faded))
                if (group.visible)
                    CoreEditorUtils.DrawPopup(content, prop, options);
        }

        void DrawRenderingSettings()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Styles.renderingLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(Styles.renderScaleLabel);
            m_RenderScale.floatValue = EditorGUILayout.Slider(m_RenderScale.floatValue, kMinRenderScale, kMaxRenderScale);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(Styles.maxPixelLightsLabel);
            m_MaxPixelLights.intValue = EditorGUILayout.IntSlider(m_MaxPixelLights.intValue, 0, kMaxSupportedPixelLights);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(m_SupportsVertexLightProp, Styles.enableVertexLightLabel);
            EditorGUILayout.PropertyField(m_RequireDepthTextureProp, Styles.requireDepthTexture);
            DrawAnimatedProperty(m_RequireSoftParticlesProp, Styles.requireSoftParticles, m_ShowSoftParticles);
            EditorGUILayout.PropertyField(m_RequireOpaqueTextureProp, Styles.requireOpaqueTexture);
            DrawAnimatedPopup(m_OpaqueDownsamplingProp, Styles.opaqueDownsampling, Styles.opaqueDownsamplingOptions, m_ShowOpaqueTextureScale);
            EditorGUILayout.PropertyField(m_HDR, Styles.hdrContent);
            EditorGUILayout.PropertyField(m_MSAA, Styles.msaaContent);

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
            EditorGUILayout.Space();
        }

        void DrawShadowSettings()
        {
            EditorGUILayout.LabelField(Styles.shadowLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(m_DirectionalShadowsSupportedProp, Styles.supportsDirectionalShadows);
            bool directionalShadows = m_DirectionalShadowsSupportedProp.boolValue;
            if (directionalShadows)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_DirectionalShadowAtlasResolutionProp, Styles.directionalShadowAtlasResolution);
                m_ShadowDistanceProp.floatValue = Mathf.Max(0.0f,
                        EditorGUILayout.FloatField(Styles.shadowDistance, m_ShadowDistanceProp.floatValue));
                CoreEditorUtils.DrawPopup(Styles.shadowCascades, m_ShadowCascadesProp, Styles.shadowCascadeOptions);

                ShadowCascades cascades = (ShadowCascades)m_ShadowCascadesProp.intValue;
                if (cascades == ShadowCascades.FOUR_CASCADES)
                    CoreEditorUtils.DrawCascadeSplitGUI<Vector3>(ref m_ShadowCascade4SplitProp);
                else if (cascades == ShadowCascades.TWO_CASCADES)
                    CoreEditorUtils.DrawCascadeSplitGUI<float>(ref m_ShadowCascade2SplitProp);

                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }

            EditorGUILayout.PropertyField(m_LocalShadowSupportedProp, Styles.supportsLocalShadows);
            bool localShadows = m_LocalShadowSupportedProp.boolValue;
            if (localShadows)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_LocalShadowsAtlasResolutionProp, Styles.localShadowsAtlasResolution);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }

            if (directionalShadows || localShadows)
                EditorGUILayout.PropertyField(m_SoftShadowsSupportedProp, Styles.supportsSoftShadows);

            EditorGUI.indentLevel--;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            UpdateAnimationValues();
            DrawRenderingSettings();
            DrawShadowSettings();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
