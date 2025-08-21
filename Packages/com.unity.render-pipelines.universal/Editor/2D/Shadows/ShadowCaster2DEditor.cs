using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.Rendering.Universal;

#if USING_2DCOMMON
using UnityEditor.U2D.Common.Path;
#endif

namespace UnityEditor.Rendering.Universal
{

#if USING_2DCOMMON

    internal class ShadowCasterPath : ScriptablePath
    {
        internal Bounds GetBounds()
        {
            ShadowCaster2D shadowCaster = (ShadowCaster2D)owner;
            Renderer m_Renderer = shadowCaster.GetComponent<Renderer>();
            if (m_Renderer != null)
            {
                return m_Renderer.bounds;
            }
            else
            {
                Collider2D collider = shadowCaster.GetComponent<Collider2D>();
                if (collider != null)
                    return collider.bounds;
            }

            return new Bounds(shadowCaster.transform.position, shadowCaster.transform.lossyScale);
        }

        internal void SetDefaultShape()
        {
            Clear();
            Bounds bounds = GetBounds();

            AddPoint(new ControlPoint() {position = bounds.min});
            AddPoint(new ControlPoint() {position = new Vector3(bounds.min.x, bounds.max.y)});
            AddPoint(new ControlPoint() {position = bounds.max});
            AddPoint(new ControlPoint() {position = new Vector3(bounds.max.x, bounds.min.y)});
        }
    }

#endif

    [CustomEditor(typeof(ShadowCaster2D))]
    [CanEditMultipleObjects]
    internal class ShadowCaster2DEditor
#if USING_2DCOMMON
        : PathComponentEditor<ShadowCasterPath>
#else
        : Editor
#endif
    {

#if USING_2DCOMMON
        [EditorTool("Edit Shadow Caster Shape", typeof(ShadowCaster2D))]
        class ShadowCaster2DShadowCasterShapeTool : ShadowCaster2DShapeTool {};

#endif

        private static class Styles
        {
            public static readonly GUIContent shadowShape2DProvider = EditorGUIUtility.TrTextContent("Shadow Shape 2D Provider", "");
            public static readonly GUIContent castsShadows = EditorGUIUtility.TrTextContent("Casts Shadows", "Specifies if this renderer will cast shadows");
            public static readonly GUIContent castingSourcePrefixLabel = EditorGUIUtility.TrTextContent("Casting Source", "Specifies the source used for projected shadows");
            public static readonly GUIContent sortingLayerPrefixLabel = EditorGUIUtility.TrTextContent("Target Sorting Layers", "Apply shadows to the specified sorting layers.");
            public static readonly GUIContent shadowShapeTrim = EditorGUIUtility.TrTextContent("Trim Edge", "This contracts the edge of the shape given by the shape provider by the specified amount");
            public static readonly GUIContent alphaCutoff = EditorGUIUtility.TrTextContent("Alpha Cutoff", "Required for correct unshadowed sprite overlap.");
            public static readonly GUIContent castingOption = EditorGUIUtility.TrTextContent("Casting Option", "Specifies how to draw the shadow used with the ShadowCaster2D");
            public static readonly GUIContent castingSource = EditorGUIUtility.TrTextContent("Casting Source", "Specifies the source of the shape used for projected shadows");
            public static readonly GUIContent buttonText = EditorGUIUtility.TrTextContent("Install 2D Common Package");
            public static readonly GUIContent helpBox = EditorGUIUtility.TrTextContent("2D Common Package is required to edit ShadowCaster 2D Shape. Please install it by clicking button above");
        }

        SerializedProperty m_CastingOption;
        SerializedProperty m_CastsShadows;
        SerializedProperty m_CastingSource;
        SerializedProperty m_ShadowMesh;
        SerializedProperty m_TrimEdge;
        SerializedProperty m_AlphaCutoff;
        SerializedProperty m_ShadowShape2DProvider;
        SortingLayerDropDown m_SortingLayerDropDown;
        CastingSourceDropDown m_CastingSourceDropDown;

        public void OnEnable()
        {
            m_CastingOption = serializedObject.FindProperty("m_CastingOption");
            m_CastsShadows = serializedObject.FindProperty("m_CastsShadows");
            m_CastingSource = serializedObject.FindProperty("m_ShadowCastingSource");
            m_ShadowMesh = serializedObject.FindProperty("m_ShadowMesh");
            m_AlphaCutoff = serializedObject.FindProperty("m_AlphaCutoff");
            m_TrimEdge = m_ShadowMesh.FindPropertyRelative("m_TrimEdge");
            m_ShadowShape2DProvider = serializedObject.FindProperty("m_ShadowShape2DProvider");

            m_SortingLayerDropDown = new SortingLayerDropDown();
            m_SortingLayerDropDown.OnEnable(serializedObject, "m_ApplyToSortingLayers");

            m_CastingSourceDropDown = new CastingSourceDropDown();
        }

        public void ShadowCaster2DSceneGUI()
        {
            ShadowCaster2D shadowCaster = target as ShadowCaster2D;

            Transform t = shadowCaster.transform;
            shadowCaster.DrawPreviewOutline();
        }

#if USING_2DCOMMON
        public void ShadowCaster2DInspectorGUI<T>() where T : ShadowCaster2DShapeTool
        {
            DoEditButton<T>(PathEditorToolContents.icon, "Edit Shape");
            DoPathInspector<T>();
        }
#endif

        public void OnSceneGUI()
        {
            if (m_CastsShadows.boolValue)
                ShadowCaster2DSceneGUI();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            m_CastingSourceDropDown.OnCastingSource(serializedObject, targets, Styles.castingSourcePrefixLabel);

            EditorGUILayout.PropertyField(m_CastingOption, Styles.castingOption);

            m_SortingLayerDropDown.OnTargetSortingLayers(serializedObject, targets, Styles.sortingLayerPrefixLabel, null);

            bool usingShapeProvider = m_CastingSource.intValue == (int)ShadowCaster2D.ShadowCastingSources.ShapeProvider;
            if (usingShapeProvider)
            {
                EditorGUILayout.PropertyField(m_TrimEdge, Styles.shadowShapeTrim);
                if (m_TrimEdge.floatValue < 0)
                    m_TrimEdge.floatValue = 0;

                EditorGUILayout.PropertyField(m_AlphaCutoff, Styles.alphaCutoff);
            }

#if USING_2DCOMMON
            if ((ShadowCaster2D.ShadowCastingSources)m_CastingSource.intValue == ShadowCaster2D.ShadowCastingSources.ShapeEditor)
                ShadowCaster2DInspectorGUI<ShadowCaster2DShadowCasterShapeTool>();
            else if (EditorToolManager.IsActiveTool<ShadowCaster2DShadowCasterShapeTool>())
                ToolManager.RestorePreviousTool();
#else
            var clicked = GUILayout.Button(Styles.buttonText);
            if (clicked)
                URP2DConverterUtility.InstallPackage("com.unity.2d.common");
            else
                EditorGUILayout.HelpBox(Styles.helpBox.text, MessageType.Info);
#endif

            if (m_ShadowShape2DProvider != null)
                EditorGUILayout.PropertyField(m_ShadowShape2DProvider, Styles.shadowShape2DProvider, true);


            serializedObject.ApplyModifiedProperties();
        }
    }

}
