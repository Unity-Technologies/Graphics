using NUnit.Framework;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using static UnityEngine.Rendering.DebugUI;



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
        class ShadowCaster2DShadowCasterShapeTool : ShadowCaster2DShapeTool { };

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
            public static readonly GUIContent none = EditorGUIUtility.TrTextContent("None");
            public static readonly GUIContent providerFoldoutLabel = EditorGUIUtility.TrTextContent("Provider");
            public static readonly GUIContent shapeEditor = EditorGUIUtility.TrTextContent("Shape Editor");
        }

        SerializedProperty m_CastingOption;
        SerializedProperty m_CastsShadows;
        SerializedProperty m_CastingSource;
        SerializedProperty m_ShadowMesh;
        SerializedProperty m_TrimEdge;
        SerializedProperty m_AlphaCutoff;
        SerializedProperty m_ShadowShape2DProvider;
        SortingLayerDropDown m_SortingLayerDropDown;
        SerializedProperty m_SelectedMenuItemID;
        SerializedProperty m_SelectionSources;

        SavedBool m_ProviderSettingsFoldout;


        public void OnEnable()
        {
            m_CastingOption = serializedObject.FindProperty("m_CastingOption");
            m_CastsShadows = serializedObject.FindProperty("m_CastsShadows");
            m_CastingSource = serializedObject.FindProperty("m_ShadowCastingSource");
            m_ShadowMesh = serializedObject.FindProperty("m_ShadowMesh");
            m_AlphaCutoff = serializedObject.FindProperty("m_AlphaCutoff");
            m_TrimEdge = m_ShadowMesh.FindPropertyRelative("m_TrimEdge");
            m_ShadowShape2DProvider = serializedObject.FindProperty("m_ShadowShape2DProvider");
            m_SelectedMenuItemID = serializedObject.FindProperty("m_SelectedMenuItemID");
            m_SelectionSources = serializedObject.FindProperty("m_SelectionSources");
            m_SortingLayerDropDown = new SortingLayerDropDown();
            m_SortingLayerDropDown.OnEnable(serializedObject, "m_ApplyToSortingLayers");

            m_ProviderSettingsFoldout = new SavedBool($"{target.GetType()}.2DURPProviderSettingsFoldout", true);
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

        public void SetCastingSourceSelection(Provider2D provider, Component component) 
        {
            serializedObject.Update();
            SerializedProperty providerProp = serializedObject.FindProperty("m_ShadowShape2DProvider");
            SerializedProperty componentProp = serializedObject.FindProperty("m_ShadowShape2DComponent");

            if (providerProp != null)
                providerProp.managedReferenceValue = provider;

            if (componentProp != null)
                componentProp.objectReferenceValue = component;

            serializedObject.ApplyModifiedProperties();
        }

#if USING_2DCOMMON
        public void RestorePreviousTool()
        {
            if (EditorToolManager.IsActiveTool<ShadowCaster2DShadowCasterShapeTool>())
                ToolManager.RestorePreviousTool();
        }

        public void DrawShapeTool()
        {
            ShadowCaster2DInspectorGUI<ShadowCaster2DShadowCasterShapeTool>();
        }
#else
        public void DrawShapeTool()
        {
            var clicked = GUILayout.Button(Styles.buttonText);
            if (clicked)
                URP2DConverterUtility.InstallPackage("com.unity.2d.common");
            else
                EditorGUILayout.HelpBox(Styles.helpBox.text, MessageType.Info);
        }

        public void RestorePreviousTool()
        {
        }
#endif

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            List<SelectionSource> additionalSources = new List<SelectionSource>
            {
                new Shadow2DSource_ShapeEditor(Styles.shapeEditor, int.MinValue),
            };

            Shadow2DProviderSources.SetAdditionalSources(m_SelectionSources, additionalSources);
            EditorGUILayout.PropertyField(m_SelectionSources, Styles.castingSourcePrefixLabel);
            Shadow2DProviderSources.SetSourceType(m_SelectionSources);

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

            serializedObject.ApplyModifiedProperties();

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

            ShadowCaster2D shadowCaster2D = target as ShadowCaster2D;
            ShadowCaster2DProvider provider = m_ShadowShape2DProvider.boxedValue as ShadowCaster2DProvider;
            if (provider != null && shadowCaster2D.shadowCastingSource == ShadowCaster2D.ShadowCastingSources.ShapeProvider)
            {
                // Draw the fold out for non
                if (Light2DEditorUtility.ContainsVisibleInspectorProperites(provider))
                {
                    bool value = CoreEditorUtils.DrawHeaderFoldout(Styles.providerFoldoutLabel, m_ProviderSettingsFoldout.value);
                    if (value)
                        Shadow2DProviderSources.DrawSelectedSourceUI(m_SelectionSources);

                    if (value != m_ProviderSettingsFoldout.value)
                        m_ProviderSettingsFoldout.value = value;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }

}
