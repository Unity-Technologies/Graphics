using UnityEditor;
using UnityEditor.AnimatedValues;

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

            public static GUIContent shadowType = new GUIContent("Type",
                    "Global shadow settings. Options are NO_SHADOW, HARD_SHADOWS and SOFT_SHADOWS.");

            public static GUIContent shadowNearPlaneOffset = new GUIContent("Near Plane Offset",
                    "Offset shadow near plane to account for large triangles being distorted by pancaking.");

            public static GUIContent shadowDistante = new GUIContent("Distance", "Max shadow rendering distance.");

            public static GUIContent shadowAtlasResolution = new GUIContent("Shadowmap Resolution",
                    "Resolution of shadow map texture. If cascades are enabled, cascades will be packed into an atlas and this setting controls the max shadows atlas resolution.");

            public static GUIContent shadowCascades = new GUIContent("Cascades",
                    "Number of cascades used in directional lights shadows");

            public static GUIContent shadowCascadeSplit = new GUIContent("Cascades Split",
                    "Percentages to split shadow volume");

            public static GUIContent hdrContent = new GUIContent("HDR", "Controls the global HDR settings.");
            public static GUIContent msaaContent = new GUIContent("Anti Aliasing (MSAA)", "Controls the global anti aliasing settings.");

            public static string[] shadowTypeOptions = {"No Shadows", "Hard Shadows", "Hard and Soft Shadows"};
            public static string[] shadowCascadeOptions = {"No Cascades", "Two Cascades", "Four Cascades"};
        }

        AnimBool m_ShowSoftParticles = new AnimBool();

        private int kMaxSupportedPixelLights = 8;
        private float kMinRenderScale = 0.1f;
        private float kMaxRenderScale = 4.0f;
        private SerializedProperty m_RenderScale;
        private SerializedProperty m_MaxPixelLights;
        private SerializedProperty m_SupportsVertexLightProp;
        private SerializedProperty m_RequireDepthTextureProp;
        private SerializedProperty m_RequireSoftParticlesProp;
        private SerializedProperty m_ShadowTypeProp;
        private SerializedProperty m_ShadowNearPlaneOffsetProp;
        private SerializedProperty m_ShadowDistanceProp;
        private SerializedProperty m_ShadowAtlasResolutionProp;
        private SerializedProperty m_ShadowCascadesProp;
        private SerializedProperty m_ShadowCascade2SplitProp;
        private SerializedProperty m_ShadowCascade4SplitProp;
        private SerializedProperty m_HDR;
        private SerializedProperty m_MSAA;

        void OnEnable()
        {
            m_RenderScale = serializedObject.FindProperty("m_RenderScale");
            m_MaxPixelLights = serializedObject.FindProperty("m_MaxPixelLights");
            m_SupportsVertexLightProp = serializedObject.FindProperty("m_SupportsVertexLight");
            m_RequireDepthTextureProp = serializedObject.FindProperty("m_RequireDepthTexture");
            m_RequireSoftParticlesProp = serializedObject.FindProperty("m_RequireSoftParticles");
            m_ShadowTypeProp = serializedObject.FindProperty("m_ShadowType");
            m_ShadowNearPlaneOffsetProp = serializedObject.FindProperty("m_ShadowNearPlaneOffset");
            m_ShadowDistanceProp = serializedObject.FindProperty("m_ShadowDistance");
            m_ShadowAtlasResolutionProp = serializedObject.FindProperty("m_ShadowAtlasResolution");
            m_ShadowCascadesProp = serializedObject.FindProperty("m_ShadowCascades");
            m_ShadowCascade2SplitProp = serializedObject.FindProperty("m_Cascade2Split");
            m_ShadowCascade4SplitProp = serializedObject.FindProperty("m_Cascade4Split");
            m_HDR = serializedObject.FindProperty("m_SupportsHDR");
            m_MSAA = serializedObject.FindProperty("m_MSAA");

            m_ShowSoftParticles.valueChanged.AddListener(Repaint);
            m_ShowSoftParticles.value = m_RequireSoftParticlesProp.boolValue;
        }

        void OnDisable()
        {
            m_ShowSoftParticles.valueChanged.RemoveListener(Repaint);
        }

        void UpdateAnimationValues()
        {
            m_ShowSoftParticles.target = m_RequireDepthTextureProp.boolValue;
        }

        void DrawAnimatedProperty(SerializedProperty prop, GUIContent content, AnimBool animation)
        {
            using (var group = new EditorGUILayout.FadeGroupScope(animation.faded))
                if (group.visible)
                    EditorGUILayout.PropertyField(prop, content);
        }

        protected void DoPopup(GUIContent label, SerializedProperty property, string[] options)
        {
            var mode = property.intValue;
            EditorGUI.BeginChangeCheck();

            if (mode >= options.Length)
                Debug.LogError(string.Format("Invalid option while trying to set {0}", label.text));

            mode = EditorGUILayout.Popup(label, mode, options);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(property.objectReferenceValue, property.name);
                property.intValue = mode;
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            UpdateAnimationValues();
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
            EditorGUILayout.PropertyField(m_HDR, Styles.hdrContent);
            EditorGUILayout.PropertyField(m_MSAA, Styles.msaaContent);

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField(Styles.shadowLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            DoPopup(Styles.shadowType, m_ShadowTypeProp, Styles.shadowTypeOptions);
            EditorGUILayout.PropertyField(m_ShadowAtlasResolutionProp, Styles.shadowAtlasResolution);

            EditorGUILayout.PropertyField(m_ShadowNearPlaneOffsetProp, Styles.shadowNearPlaneOffset);
            EditorGUILayout.PropertyField(m_ShadowDistanceProp, Styles.shadowDistante);
            DoPopup(Styles.shadowCascades, m_ShadowCascadesProp, Styles.shadowCascadeOptions);

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

            serializedObject.ApplyModifiedProperties();
        }
    }
}
