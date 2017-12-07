using UnityEditor;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    [CustomEditor(typeof(LightweightPipelineAsset))]
    public class LightweightAssetEditor : Editor
    {
        internal class Styles
        {
            public static GUIContent renderingLabel = new GUIContent("Rendering");
            public static GUIContent shadowLabel = new GUIContent("Shadows");
            public static GUIContent defaults = new GUIContent("Defaults");

            public static GUIContent renderScaleLabel = new GUIContent("Render Scale", "Allows game to render at a resolution different than native resolution. UI is always rendered at native resolution.");

            public static GUIContent maxPixelLightsLabel = new GUIContent("Max Pixel Lights",
                    "Controls the amount of pixel lights that run in fragment light loop. Lights are sorted and culled per-object.");

            public static GUIContent enableVertexLightLabel = new GUIContent("Enable Vertex Light",
                    "If enabled, shades additional lights exceeding maxAdditionalPixelLights per-vertex up to the maximum of 8 lights.");

            public static GUIContent requireCameraDepthTexture = new GUIContent("Camera Depth Texture", "If enabled the the pipeline will generate depth texture necessary for some effects like soft particles.");

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

            public static GUIContent defaultMaterial = new GUIContent("Default Material",
                "Material to use when creating 3D objects");

            public static GUIContent defaultParticleMaterial = new GUIContent("Default Particle Material",
                "Material to use when creating Particle Systems");

            public static GUIContent defaultTerrainMaterial = new GUIContent("Default Terrain Material",
                "Material to use in Terrains");

            public static GUIContent msaaContent = new GUIContent("Anti Aliasing (MSAA)", "Controls the global anti aliasing applied to all cameras.");
        }

        private int kMaxSupportedPixelLights = 8;
        private SerializedProperty m_RenderScale;
        private SerializedProperty m_MaxPixelLights;
        private SerializedProperty m_SupportsVertexLightProp;
        private SerializedProperty m_RequireCameraDepthTextureProp;
        private SerializedProperty m_ShadowTypeProp;
        private SerializedProperty m_ShadowNearPlaneOffsetProp;
        private SerializedProperty m_ShadowDistanceProp;
        private SerializedProperty m_ShadowAtlasResolutionProp;
        private SerializedProperty m_ShadowCascadesProp;
        private SerializedProperty m_ShadowCascade2SplitProp;
        private SerializedProperty m_ShadowCascade4SplitProp;
        private SerializedProperty m_DefaultMaterial;
        private SerializedProperty m_DefaultParticleMaterial;
        private SerializedProperty m_DefaultTerrainMaterial;
        private SerializedProperty m_MSAA;

        void OnEnable()
        {
            m_RenderScale = serializedObject.FindProperty("m_RenderScale");
            m_MaxPixelLights = serializedObject.FindProperty("m_MaxPixelLights");
            m_SupportsVertexLightProp = serializedObject.FindProperty("m_SupportsVertexLight");
            m_RequireCameraDepthTextureProp = serializedObject.FindProperty("m_RequireCameraDepthTexture");
            m_ShadowTypeProp = serializedObject.FindProperty("m_ShadowType");
            m_ShadowNearPlaneOffsetProp = serializedObject.FindProperty("m_ShadowNearPlaneOffset");
            m_ShadowDistanceProp = serializedObject.FindProperty("m_ShadowDistance");
            m_ShadowAtlasResolutionProp = serializedObject.FindProperty("m_ShadowAtlasResolution");
            m_ShadowCascadesProp = serializedObject.FindProperty("m_ShadowCascades");
            m_ShadowCascade2SplitProp = serializedObject.FindProperty("m_Cascade2Split");
            m_ShadowCascade4SplitProp = serializedObject.FindProperty("m_Cascade4Split");
            m_DefaultMaterial = serializedObject.FindProperty("m_DefaultMaterial");
            m_DefaultParticleMaterial = serializedObject.FindProperty("m_DefaultParticleMaterial");
            m_DefaultTerrainMaterial = serializedObject.FindProperty("m_DefaultTerrainMaterial");
            m_MSAA = serializedObject.FindProperty("m_MSAA");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Styles.renderingLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(Styles.renderScaleLabel);
            m_RenderScale.floatValue = EditorGUILayout.Slider(m_RenderScale.floatValue, 0.1f, 1.0f);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(Styles.maxPixelLightsLabel);
            m_MaxPixelLights.intValue = EditorGUILayout.IntSlider(m_MaxPixelLights.intValue, 0, kMaxSupportedPixelLights);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(m_SupportsVertexLightProp, Styles.enableVertexLightLabel);
            EditorGUILayout.PropertyField(m_RequireCameraDepthTextureProp, Styles.requireCameraDepthTexture);
            EditorGUILayout.PropertyField(m_MSAA, Styles.msaaContent);

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
            EditorGUILayout.PropertyField(m_DefaultMaterial, Styles.defaultMaterial);
            EditorGUILayout.PropertyField(m_DefaultParticleMaterial, Styles.defaultParticleMaterial);
            EditorGUILayout.PropertyField(m_DefaultTerrainMaterial, Styles.defaultTerrainMaterial);
            EditorGUI.indentLevel--;

            serializedObject.ApplyModifiedProperties();
        }
    }
}
