using UnityEditor.EditorTools;
using UnityEditor.Rendering.Universal.Path2D;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
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

        public override void SetDefaultShape()
        {
            Clear();
            Bounds bounds = GetBounds();

            AddPoint(new ControlPoint(bounds.min));
            AddPoint(new ControlPoint(new Vector3(bounds.min.x, bounds.max.y)));
            AddPoint(new ControlPoint(bounds.max));
            AddPoint(new ControlPoint(new Vector3(bounds.max.x, bounds.min.y)));

            base.SetDefaultShape();
        }
    }


    [CustomEditor(typeof(ShadowCaster2D))]
    [CanEditMultipleObjects]
    internal class ShadowCaster2DEditor : PathComponentEditor<ShadowCasterPath>
    {
        [EditorTool("Edit Shadow Caster Shape", typeof(ShadowCaster2D))]
        class ShadowCaster2DShadowCasterShapeTool : ShadowCaster2DShapeTool {};

        private static class Styles
        {
            public static GUIContent shadowShape2DProvider = EditorGUIUtility.TrTextContent("Shadow Shape 2D Provider", "");
            public static GUIContent castsShadows = EditorGUIUtility.TrTextContent("Casts Shadows", "Specifies if this renderer will cast shadows");
            public static GUIContent castingSourcePrefixLabel = EditorGUIUtility.TrTextContent("Casting Source", "Specifies the source used for projected shadows");
            public static GUIContent sortingLayerPrefixLabel = EditorGUIUtility.TrTextContent("Target Sorting Layers", "Apply shadows to the specified sorting layers.");
            public static GUIContent shadowShapeTrim = EditorGUIUtility.TrTextContent("Trim Edge", "This contracts the edge of the shape given by the shape provider by the specified amount");
            public static GUIContent alphaCutoff = EditorGUIUtility.TrTextContent("Alpha Cutoff", "Required for correct unshadowed sprite overlap.");
            public static GUIContent castingOption = EditorGUIUtility.TrTextContent("Casting Option", "Specifies how to draw the shadow used with the ShadowCaster2D");
            public static GUIContent castingSource = EditorGUIUtility.TrTextContent("Casting Source", "Specifies the source of the shape used for projected shadows");
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

        public void ShadowCaster2DInspectorGUI<T>() where T : ShadowCaster2DShapeTool
        {
            DoEditButton<T>(PathEditorToolContents.icon, "Edit Shape");
            DoPathInspector<T>();
            DoSnappingInspector<T>();
        }

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

            if ((ShadowCaster2D.ShadowCastingSources)m_CastingSource.intValue == ShadowCaster2D.ShadowCastingSources.ShapeEditor)
                ShadowCaster2DInspectorGUI<ShadowCaster2DShadowCasterShapeTool>();
            else if (EditorToolManager.IsActiveTool<ShadowCaster2DShadowCasterShapeTool>())
                ToolManager.RestorePreviousTool();

            if(m_ShadowShape2DProvider != null)
                EditorGUILayout.PropertyField(m_ShadowShape2DProvider, Styles.shadowShape2DProvider, true);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
