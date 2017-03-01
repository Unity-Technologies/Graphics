using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LowEndRenderPipeline))]
public class LDRenderPipelineInspector : Editor
{
    internal class Styles
    {
        public static GUIContent renderingLabel = new GUIContent("Rendering");
        public static GUIContent shadowLabel = new GUIContent("Shadows");

        public static GUIContent enableVertexLightLabel = new GUIContent("Enable Vertex Light");
        public static GUIContent enableLightmap = new GUIContent("Enable Lightmap");
        public static GUIContent enableAmbientProbe = new GUIContent("Enable Ambient Probe");
        public static GUIContent shadowType = new GUIContent("Shadow Type");
        public static GUIContent shadowNearPlaneOffset = new GUIContent("Shadow Near Plane Offset");
        public static GUIContent shadowDistante = new GUIContent("Shadow Distance");
        public static GUIContent shadowAtlasResolution = new GUIContent("Shadow Atlas Resolution");
        public static GUIContent shadowCascades = new GUIContent("Shadow Cascades");
        public static GUIContent shadowCascadeSplit = new GUIContent("Shadow Cascade Split");
        public static GUIContent shadowFiltering = new GUIContent("Shadow Filtering");
    }

    private SerializedProperty m_SupportsVertexLightProp;
    private SerializedProperty m_EnableLightmapsProp;
    private SerializedProperty m_EnableAmbientProbeProp;
    private SerializedProperty m_ShadowTypeProp;
    private SerializedProperty m_ShadowNearPlaneOffsetProp;
    private SerializedProperty m_ShadowDistanceProp;
    private SerializedProperty m_ShadowAtlasResolutionProp;
    private SerializedProperty m_ShadowCascadesProp;
    private SerializedProperty m_CascadeSplitProp;
    private SerializedProperty m_ShadowFilteringProp;

    void OnEnable()
    {
        m_SupportsVertexLightProp = serializedObject.FindProperty("m_SupportsVertexLight");
        m_EnableLightmapsProp = serializedObject.FindProperty("m_EnableLightmaps");
        m_EnableAmbientProbeProp = serializedObject.FindProperty("m_EnableAmbientProbe");
        m_ShadowTypeProp = serializedObject.FindProperty("m_ShadowType");
        m_ShadowNearPlaneOffsetProp = serializedObject.FindProperty("m_ShadowNearPlaneOffset");
        m_ShadowDistanceProp = serializedObject.FindProperty("m_ShadowDistance");
        m_ShadowAtlasResolutionProp = serializedObject.FindProperty("m_ShadowAtlasResolution");
        m_ShadowCascadesProp = serializedObject.FindProperty("m_ShadowCascades");
        m_CascadeSplitProp = serializedObject.FindProperty("m_CascadeSplit");
        m_ShadowFilteringProp = serializedObject.FindProperty("m_ShadowFiltering");
    }

    public override void OnInspectorGUI()
    {
        LowEndRenderPipeline pipeAsset = target as LowEndRenderPipeline;
        serializedObject.Update();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField(Styles.renderingLabel, EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(m_SupportsVertexLightProp, Styles.enableVertexLightLabel);
        EditorGUILayout.PropertyField(m_EnableLightmapsProp, Styles.enableLightmap);
        EditorGUILayout.PropertyField(m_EnableAmbientProbeProp, Styles.enableAmbientProbe);
        EditorGUI.indentLevel--;
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        EditorGUILayout.LabelField(Styles.shadowLabel, EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(m_ShadowTypeProp, Styles.shadowType);
        EditorGUILayout.PropertyField(m_ShadowNearPlaneOffsetProp, Styles.shadowNearPlaneOffset);
        EditorGUILayout.PropertyField(m_ShadowDistanceProp, Styles.shadowDistante);
        EditorGUILayout.PropertyField(m_ShadowAtlasResolutionProp, Styles.shadowAtlasResolution);
        EditorGUILayout.PropertyField(m_ShadowCascadesProp, Styles.shadowCascades);
        EditorGUILayout.PropertyField(m_CascadeSplitProp, Styles.shadowCascadeSplit);
        EditorGUILayout.PropertyField(m_ShadowFilteringProp, Styles.shadowFiltering);
        EditorGUI.indentLevel--;

        serializedObject.ApplyModifiedProperties();
    }
}
