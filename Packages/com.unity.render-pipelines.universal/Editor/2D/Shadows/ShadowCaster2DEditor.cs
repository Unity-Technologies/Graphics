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
        class ShadowCaster2DShadowCasterShapeTool : ShadowCaster2DShapeTool
        {
            public override bool IsAvailable()
            {
                if (!base.IsAvailable())
                    return false;

                foreach (var obj in targets)
                {
                    var shadowCaster = obj as ShadowCaster2D;
                    if (shadowCaster != null && shadowCaster.shadowCastingSource != ShadowCaster2D.ShadowCastingSources.ShapeEditor)
                        return false;
                }

                return true;
            }
        };

#endif

        private static class Styles
        {
            public static readonly GUIContent shadowShape2DProvider = L10n.TextContent("Shadow Shape 2D Provider", "", null, null);
            public static readonly GUIContent castsShadows = L10n.TextContent("Casts Shadows", "Specifies if this renderer will cast shadows", null, null);
            public static readonly GUIContent castingSourcePrefixLabel = L10n.TextContent("Casting Source", "Specifies the source used for projected shadows", null, null);
            public static readonly GUIContent sortingLayerPrefixLabel = L10n.TextContent("Target Sorting Layers", "Apply shadows to the specified sorting layers.", null, null);
            public static readonly GUIContent shadowShapeTrim = L10n.TextContent("Trim Edge", "This contracts the edge of the shape given by the shape provider by the specified amount", null, null);
            public static readonly GUIContent alphaCutoff = L10n.TextContent("Alpha Cutoff", "Required for correct unshadowed sprite overlap.", null, null);
            public static readonly GUIContent castingOption = L10n.TextContent("Casting Option", "Specifies how to draw the shadow used with the ShadowCaster2D", null, null);
            public static readonly GUIContent buttonText = L10n.TextContent("Install 2D Common Package", null, null, null);
            public static readonly GUIContent helpBox = L10n.TextContent("2D Common Package is required to edit ShadowCaster 2D Shape. Please install it by clicking button above", null, null, null);
            public static readonly GUIContent none = L10n.TextContent("None", null, null, null);
            public static readonly GUIContent providerFoldoutLabel = L10n.TextContent("Provider", null, null, null);
            public static readonly GUIContent shapeEditor = L10n.TextContent("Shape Editor", null, null, null);
        }

        SerializedProperty m_CastingOption;
        SerializedProperty m_CastingSource;
        SerializedProperty m_ShadowMesh;
        SerializedProperty m_TrimEdge;
        SerializedProperty m_AlphaCutoff;
        SerializedProperty m_ShadowShape2DProvider;
        SortingLayerDropDown m_SortingLayerDropDown;
        SerializedProperty m_SelectionSources;

        SavedBool m_ProviderSettingsFoldout;


        public void OnEnable()
        {
            m_CastingOption = serializedObject.FindProperty("m_CastingOption");
            m_CastingSource = serializedObject.FindProperty("m_ShadowCastingSource");
            m_ShadowMesh = serializedObject.FindProperty("m_ShadowMesh");
            m_AlphaCutoff = serializedObject.FindProperty("m_AlphaCutoff");
            m_TrimEdge = m_ShadowMesh.FindPropertyRelative("m_TrimEdge");

            m_ShadowShape2DProvider = serializedObject.FindProperty("m_ShadowShape2DProvider");
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
            // Gated on the castsShadows property, not the legacy m_CastsShadows field -- nothing
            // has written that field since Version_2 folded it into m_CastingOption.
            ShadowCaster2D shadowCaster = target as ShadowCaster2D;
            if (shadowCaster != null && shadowCaster.castsShadows)
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
            if (U2D.Common.Path.EditorToolManager.IsActiveTool<ShadowCaster2DShadowCasterShapeTool>())
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

            // Cannot use the serialized property m_SelectionSources here as setting the additional sources is only applied to a single object.
            for (int i=0;i<targets.Length;i++)
            {
                ShadowCaster2D target = targets[i] as ShadowCaster2D;
                SerializedObject targetObj = new SerializedObject(target);
                SerializedProperty selectionSources = targetObj.FindProperty("m_SelectionSources");
                Shadow2DProviderSources.SetAdditionalSources(selectionSources, additionalSources);
            }


            // The property drawer applies the pick to each selected caster through that caster's own
            // SerializedObject. Applying it again here through the shared one would write the first
            // caster's source Component onto every other selected caster, pointing them at a component
            // on a GameObject that is not theirs. The shared object does need re-reading though, or the
            // rest of this inspector draws the casting source as it was before the pick.
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_SelectionSources, Styles.castingSourcePrefixLabel);
            if (EditorGUI.EndChangeCheck())
                serializedObject.Update();

            EditorGUILayout.PropertyField(m_CastingOption, Styles.castingOption);

            // No Material field and no Geometry Generation popup.
            //
            // Both moved out from under the caster. The shadow material is the LIGHT's
            // (Light2D.shadowMaterial): one material serves every caster a light reaches, so the
            // four-phase stencil handshake runs against one shader per light rather than one per
            // caster. The geometry generator is the PROJECT's
            // (Shadow2DGeometrySettings.geometryVersion): a generator's id implies a vertex layout, and
            // a layout is only meaningful paired with a shader that reads it, so making it a project
            // decision is what lets any caster and any light material be compatible by construction.
            //
            // What used to be reported here -- a material missing shadow passes, a caster whose
            // generator its material cannot read -- is therefore either impossible now or belongs to
            // Light2DEditor, which reports the missing-pass case.

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
            else if (U2D.Common.Path.EditorToolManager.IsActiveTool<ShadowCaster2DShadowCasterShapeTool>())
                ToolManager.RestorePreviousTool();
#else
            var clicked = GUILayout.Button(Styles.buttonText);
            if (clicked)
                URP2DConverterUtility.InstallPackage("com.unity.2d.common");
            else
                EditorGUILayout.HelpBox(Styles.helpBox.text, MessageType.Info);
#endif

            ShadowCaster2D shadowCaster2D = target as ShadowCaster2D;
            ShadowShape2DProvider provider = m_ShadowShape2DProvider.boxedValue as ShadowShape2DProvider;
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
