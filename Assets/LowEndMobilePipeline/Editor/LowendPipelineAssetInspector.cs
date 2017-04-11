using UnityEditor;

namespace UnityEngine.Experimental.Rendering.LowendMobile
{
    [CustomEditor(typeof(LowEndMobilePipelineAsset))]
    public class LowendPipelineAssetInspector : Editor
    {
        internal class Styles
        {
            public static GUIContent renderingLabel = new GUIContent("Rendering");
            public static GUIContent shadowLabel = new GUIContent("Shadows");

            public static GUIContent maxPixelLights = new GUIContent("Max per-pixel lights supported",
                    "Amount of dynamic lights processed in fragment shader. More than 1 per-pixel light is not recommended.");

            public static GUIContent enableVertexLightLabel = new GUIContent("Enable Vertex Light",
                    "Enable up to 4 per-vertex dynamic lights.");

            public static GUIContent enableLightmap = new GUIContent("Enable Lightmap",
                    "Only non-directional lightmaps are supported");

            public static GUIContent enableAmbientProbe = new GUIContent("Enable Ambient Probe",
                    "Uses light probes as ambient light source for non-lightmapped objects.");

            public static GUIContent shadowType = new GUIContent("Shadow Type",
                    "Single directional shadow supported. SOFT_SHADOWS applies shadow filtering.");

            public static GUIContent shadowNearPlaneOffset = new GUIContent("Shadow Near Plane Offset",
                    "Offset shadow near plane to account for large triangles being distorted by pancaking");

            public static GUIContent shadowDistante = new GUIContent("Shadow Distance", "Max shadow drawing distance");
            public static GUIContent shadowBias = new GUIContent("Shadow Bias");

            public static GUIContent shadowAtlasResolution = new GUIContent("Shadow Map Resolution",
                    "Resolution of shadow map texture. If cascades are enabled all cascades will be packed into this texture resolution.");

            public static GUIContent shadowCascades = new GUIContent("Shadow Cascades",
                    "Number of cascades for directional shadows");

            public static GUIContent shadowCascadeSplit = new GUIContent("Shadow Cascade Split",
                    "Percentages to split shadow volume");
        }

        private SerializedProperty m_MaxPixelLights;
        private SerializedProperty m_SupportsVertexLightProp;
        private SerializedProperty m_EnableLightmapsProp;
        private SerializedProperty m_EnableAmbientProbeProp;
        private SerializedProperty m_ShadowTypeProp;
        private SerializedProperty m_ShadowNearPlaneOffsetProp;
        private SerializedProperty m_ShadowBiasProperty;
        private SerializedProperty m_ShadowDistanceProp;
        private SerializedProperty m_ShadowAtlasResolutionProp;
        private SerializedProperty m_ShadowCascadesProp;
        private SerializedProperty m_ShadowCascade2SplitProp;
        private SerializedProperty m_ShadowCascade4SplitProp;

        void OnEnable()
        {
            m_MaxPixelLights = serializedObject.FindProperty("m_MaxPixelLights");
            m_SupportsVertexLightProp = serializedObject.FindProperty("m_SupportsVertexLight");
            m_EnableLightmapsProp = serializedObject.FindProperty("m_EnableLightmaps");
            m_EnableAmbientProbeProp = serializedObject.FindProperty("m_EnableAmbientProbe");
            m_ShadowTypeProp = serializedObject.FindProperty("m_ShadowType");
            m_ShadowNearPlaneOffsetProp = serializedObject.FindProperty("m_ShadowNearPlaneOffset");
            m_ShadowBiasProperty = serializedObject.FindProperty("m_ShadowBias");
            m_ShadowDistanceProp = serializedObject.FindProperty("m_ShadowDistance");
            m_ShadowAtlasResolutionProp = serializedObject.FindProperty("m_ShadowAtlasResolution");
            m_ShadowCascadesProp = serializedObject.FindProperty("m_ShadowCascades");
            m_ShadowCascade2SplitProp = serializedObject.FindProperty("m_Cascade2Split");
            m_ShadowCascade4SplitProp = serializedObject.FindProperty("m_Cascade4Split");
        }

        public override void OnInspectorGUI()
        {
            LowEndMobilePipelineAsset pipeAsset = target as LowEndMobilePipelineAsset;
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Styles.renderingLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_MaxPixelLights, Styles.maxPixelLights);
            EditorGUILayout.PropertyField(m_SupportsVertexLightProp, Styles.enableVertexLightLabel);
            EditorGUILayout.PropertyField(m_EnableLightmapsProp, Styles.enableLightmap);
            EditorGUILayout.PropertyField(m_EnableAmbientProbeProp, Styles.enableAmbientProbe);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField(Styles.shadowLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_ShadowTypeProp, Styles.shadowType);
            EditorGUILayout.PropertyField(m_ShadowAtlasResolutionProp, Styles.shadowAtlasResolution);
            EditorGUILayout.PropertyField(m_ShadowNearPlaneOffsetProp, Styles.shadowNearPlaneOffset);
            EditorGUILayout.PropertyField(m_ShadowBiasProperty, Styles.shadowBias);
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

            serializedObject.ApplyModifiedProperties();
        }
    }
}
