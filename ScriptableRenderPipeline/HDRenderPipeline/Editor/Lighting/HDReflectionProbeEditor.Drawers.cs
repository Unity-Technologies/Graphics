using UnityEditor.Experimental.Rendering;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor
{
    partial class HDReflectionProbeEditor
    {
        static void Drawer_NOOP(UIState s, SerializedReflectionProbe p, Editor owner) { }

        #region Field Drawers
        static readonly GUIContent[] k_Content_ReflectionProbeMode = { new GUIContent("Baked"), new GUIContent("Custom"), new GUIContent("Realtime") };
        static readonly int[] k_Content_ReflectionProbeModeValues = { (int)ReflectionProbeMode.Baked, (int)ReflectionProbeMode.Custom, (int)ReflectionProbeMode.Realtime };
        static void Drawer_ReflectionProbeMode(UIState s, SerializedReflectionProbe p, Editor owner)
        {
            EditorGUI.showMixedValue = p.mode.hasMultipleDifferentValues;
            EditorGUILayout.IntPopup(p.mode, k_Content_ReflectionProbeMode, k_Content_ReflectionProbeModeValues, CoreEditorUtils.GetContent("Type|'Baked Cubemap' uses the 'Auto Baking' mode from the Lighting window. If it is enabled then baking is automatic otherwise manual bake is needed (use the bake button below). \n'Custom' can be used if a custom cubemap is wanted. \n'Realtime' can be used to dynamically re-render the cubemap during runtime (via scripting)."));
            EditorGUI.showMixedValue = false;
            s.SetModeTarget(p.mode.intValue);
        }

        static void Drawer_InfluenceShape(UIState s, SerializedReflectionProbe p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.influenceShape, CoreEditorUtils.GetContent("Shape"));
        }

        static void Drawer_IntensityMultiplier(UIState s, SerializedReflectionProbe p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.intensityMultiplier, CoreEditorUtils.GetContent("Intensity"));
        }
        #endregion

        #region Toolbar
        static readonly EditMode.SceneViewEditMode[] k_Toolbar_SceneViewEditModes = 
        {
            EditMode.SceneViewEditMode.ReflectionProbeBox,
            EditMode.SceneViewEditMode.GridBox,
            EditMode.SceneViewEditMode.Collider,
            EditMode.SceneViewEditMode.ReflectionProbeOrigin
        };
        static GUIContent[] s_Toolbar_Contents = null;
        static GUIContent[] toolbar_Contents
        {
            get
            {
                return s_Toolbar_Contents ?? (s_Toolbar_Contents = new []
                {
                    EditorGUIUtility.IconContent("d_EditCollider", "|Modify the influence volume of the reflection probe."),
                    EditorGUIUtility.IconContent("d_PreMatCube", "|Modify the projection volume of the reflection probe."),
                    EditorGUIUtility.IconContent("d_Navigation", "|Fit the reflection probe volume to the surrounding colliders."),
                    EditorGUIUtility.IconContent("MoveTool", "|Move the selected objects.")
                });
            }
        }
        const string k_BaseSceneEditingToolText = "<color=grey>Probe Scene Editing Mode:</color> \n";
        static readonly GUIContent[] k_ToolNames =
        {
            new GUIContent(k_BaseSceneEditingToolText + "Box Influence Bounds", ""),
            new GUIContent(k_BaseSceneEditingToolText + "Box Projection Bounds", ""),
            new GUIContent(k_BaseSceneEditingToolText + "Fit Projection Volume", ""),
            new GUIContent(k_BaseSceneEditingToolText + "Probe Origin", "")
        };
        static readonly Bounds k_BoundsZero = new Bounds();
        static Bounds DummyBound() { return k_BoundsZero; }
        static Editor s_LastInteractedEditor = null;
        static void Drawer_Toolbar(UIState s, SerializedReflectionProbe p, Editor owner)
        {
            if (p.so.targetObjects.Length > 1)
                return;

            // Show the master tool selector
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.changed = false;
            var oldEditMode = EditMode.editMode;

            EditorGUI.BeginChangeCheck();
            EditMode.DoInspectorToolbar(k_Toolbar_SceneViewEditModes, toolbar_Contents, DummyBound, owner);
            if (EditorGUI.EndChangeCheck())
                s_LastInteractedEditor = owner;

            if (oldEditMode != EditMode.editMode)
            {
                switch (EditMode.editMode)
                {
                    case EditMode.SceneViewEditMode.ReflectionProbeOrigin:
                        s.shouldUpdateOldLocalSpace = true;
                        break;
                }
                // HDRP if (Toolbar.get != null)
                // HDRP  Toolbar.get.Repaint();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            // Info box for tools
            GUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUIUtility.labelWidth);
            GUILayout.BeginVertical(EditorStyles.helpBox);
            var helpText = k_BaseSceneEditingToolText;
            if (s.sceneViewEditing)
            {
                var index = ArrayUtility.IndexOf(k_Toolbar_SceneViewEditModes, EditMode.editMode);
                if (index >= 0)
                    helpText = k_ToolNames[index].text;
            }
            GUILayout.Label(helpText, EditorStyles.miniLabel);
            GUILayout.EndVertical();
            GUILayout.Space(EditorGUIUtility.fieldWidth);
            GUILayout.EndHorizontal();
        }
        #endregion

        #region Mode Specific Settings
        static void Drawer_ModeSettings(UIState s, SerializedReflectionProbe p, Editor owner)
        {
            for (var i = 0; i < k_ModeDrawers.Length; ++i)
            {
                if (EditorGUILayout.BeginFadeGroup(s.GetModeFaded((ReflectionProbeMode)i)))
                {
                    ++EditorGUI.indentLevel;
                    k_ModeDrawers[i](s, p, owner);
                    --EditorGUI.indentLevel;
                }
                EditorGUILayout.EndFadeGroup();
            }
        }

        static readonly Drawer[] k_ModeDrawers = { Drawer_NOOP, Drawer_ModeSettingsRealtime , Drawer_ModeSettingsCustom };
        static void Drawer_ModeSettingsCustom(UIState s, SerializedReflectionProbe p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.renderDynamicObjects, CoreEditorUtils.GetContent("Dynamic Objects|If enabled dynamic objects are also rendered into the cubemap"));

            p.customBakedTexture.objectReferenceValue = EditorGUILayout.ObjectField(CoreEditorUtils.GetContent("Cubemap"), p.customBakedTexture.objectReferenceValue, typeof(Cubemap), false);
        }

        static void Drawer_ModeSettingsRealtime(UIState s, SerializedReflectionProbe p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.refreshMode, CoreEditorUtils.GetContent("Refresh Mode|Controls how this probe refreshes in the Player"));
            EditorGUILayout.PropertyField(p.timeSlicingMode, CoreEditorUtils.GetContent("Time Slicing|If enabled this probe will update over several frames, to help reduce the impact on the frame rate"));
        }
        #endregion
    }
}
