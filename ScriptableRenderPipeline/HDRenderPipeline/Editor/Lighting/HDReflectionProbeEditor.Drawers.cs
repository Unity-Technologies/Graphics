using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering
{
    using CED = CoreEditorDrawer<HDReflectionProbeEditor.UIState, HDReflectionProbeEditor.SerializedReflectionProbe>;

    partial class HDReflectionProbeEditor
    {
        static readonly CED.IDrawer k_Drawer_Space = CED.Action(Drawer_Space);
        static readonly CED.IDrawer k_Drawer_NOOP = CED.Action(Drawer_NOOP);


        static void Drawer_NOOP(UIState s, SerializedReflectionProbe p, Editor owner) { }
        static void Drawer_Space(UIState s, SerializedReflectionProbe p, Editor owner) { EditorGUILayout.Space(); }

        #region Sections
        static readonly CED.IDrawer k_InfluenceVolumeSection = CED.FoldoutGroup(
            "Influence volume settings",
            (s, p, o) => p.blendDistance,
            CED.FadeGroup(
                (s, p, o, i) => s.GetShapeFaded((ReflectionInfluenceShape)i),
                CED.Action(Drawer_InfluenceBoxSettings),      // Box
                CED.Action(Drawer_InfluenceSphereSettings)    // Sphere
            ),
            CED.Action(Drawer_UseSeparateProjectionVolume)
        );

        static readonly CED.IDrawer[] k_PrimarySection =
        {
            CED.Action(Drawer_ReflectionProbeMode),
            CED.FadeGroup((s, p, o, i) => s.GetModeFaded((ReflectionProbeMode)i), 
                k_Drawer_NOOP,                                      // Baked
                CED.Action(Drawer_ModeSettingsRealtime),      // Realtime
                CED.Action(Drawer_ModeSettingsCustom)         // Custom
            ),
            k_Drawer_Space,
            CED.Action(Drawer_InfluenceShape),
            CED.Action(Drawer_IntensityMultiplier),
            k_Drawer_Space,
            CED.Action(Drawer_Toolbar),
            k_Drawer_Space
        };

        void Draw(CED.IDrawer[] drawers, UIState s, SerializedReflectionProbe p, Editor owner)
        {
            for (var i = 0; i < drawers.Length; i++)
                drawers[i].Draw(s, p, owner);
        }
        #endregion

        static void Drawer_InfluenceBoxSettings(UIState s, SerializedReflectionProbe p, Editor owner)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(p.boxSize, CoreEditorUtils.GetContent("Box Size|The size of the box in which the reflections will be applied to objects. The value is not affected by the Transform of the Game Object."));
            EditorGUILayout.PropertyField(p.boxOffset, CoreEditorUtils.GetContent("Box Offset|The center of the box in which the reflections will be applied to objects. The value is relative to the position of the Game Object."));

            if (EditorGUI.EndChangeCheck())
            {
                var center = p.boxSize.vector3Value;
                var size = p.boxOffset.vector3Value;
                if (ValidateAABB((ReflectionProbe)p.so.targetObject, ref center, ref size))
                {
                    p.boxOffset.vector3Value = center;
                    p.boxSize.vector3Value = size;
                }
            }
        }

        static void Drawer_InfluenceSphereSettings(UIState s, SerializedReflectionProbe p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.influenceSphereRadius, CoreEditorUtils.GetContent("Radius"));
        }

        static void Drawer_UseSeparateProjectionVolume(UIState s, SerializedReflectionProbe p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.useSeparateProjectionVolume);
        }

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
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = p.influenceShape.hasMultipleDifferentValues;
            EditorGUILayout.PropertyField(p.influenceShape, CoreEditorUtils.GetContent("Shape"));
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
                s.SetShapeTarget(p.influenceShape.intValue);
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
            EditMode.SceneViewEditMode.ReflectionProbeOrigin
        };
        static GUIContent[] s_Toolbar_Contents = null;
        static GUIContent[] toolbar_Contents
        {
            get
            {
                return s_Toolbar_Contents ?? (s_Toolbar_Contents = new []
                {
                    EditorGUIUtility.IconContent("EditCollider", "|Modify the influence volume of the reflection probe."),
                    EditorGUIUtility.IconContent("PreMatCube", "|Modify the projection volume of the reflection probe."),
                    EditorGUIUtility.IconContent("MoveTool", "|Move the selected objects.")
                });
            }
        }
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

            if (GUILayout.Button(EditorGUIUtility.IconContent("Navigation", "|Fit the reflection probe volume to the surrounding colliders.")))
                s.AddOperation(Operation.FitVolumeToSurroundings);

            if (oldEditMode != EditMode.editMode)
            {
                switch (EditMode.editMode)
                {
                    case EditMode.SceneViewEditMode.ReflectionProbeOrigin:
                        s.AddOperation(Operation.UpdateOldLocalSpace);
                        break;
                }
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
        #endregion

        #region Mode Specific Settings
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
