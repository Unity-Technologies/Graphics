using System;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using _ = CoreEditorUtils;
    using CED = CoreEditorDrawer<HDProbeUI, SerializedHDProbe>;

    partial class HDProbeUI
    {
        public static readonly CED.IDrawer SectionProbeModeSettings;
        public static readonly CED.IDrawer ProxyVolumeSettings = CED.FoldoutGroup(
                proxySettingsHeader,
                (s, d, o) => s.isSectionExpendedProxyVolume,
                FoldoutOption.Indent,
                CED.Action(Drawer_SectionProxySettings)
                );
        public static readonly CED.IDrawer SectionProbeModeBakedSettings = CED.noop;
        public static readonly CED.IDrawer SectionProbeModeRealtimeSettings = CED.Action(Drawer_SectionProbeModeRealtimeSettings);
        public static readonly CED.IDrawer SectionBakeButton = CED.Action(Drawer_SectionBakeButton);

        public static readonly CED.IDrawer SectionFoldoutAdditionalSettings = CED.FoldoutGroup(
                additionnalSettingsHeader,
                (s, d, o) => s.isSectionExpendedAdditionalSettings,
                FoldoutOption.Indent,
                CED.Action(Drawer_SectionCustomSettings)
                );

        static HDProbeUI()
        {
            SectionProbeModeSettings = CED.Group(
                    CED.Action(Drawer_FieldCaptureType),
                    CED.FadeGroup(
                        (s, d, o, i) => s.IsSectionExpandedReflectionProbeMode((ReflectionProbeMode)i),
                        FadeOption.Indent,
                        SectionProbeModeBakedSettings,
                        SectionProbeModeRealtimeSettings
                        )
                    );
        }

        protected const EditMode.SceneViewEditMode EditBaseShape = EditMode.SceneViewEditMode.ReflectionProbeBox;
        protected const EditMode.SceneViewEditMode EditInfluenceShape = EditMode.SceneViewEditMode.GridBox;
        protected const EditMode.SceneViewEditMode EditInfluenceNormalShape = EditMode.SceneViewEditMode.Collider;
        protected const EditMode.SceneViewEditMode EditCenter = EditMode.SceneViewEditMode.ReflectionProbeOrigin;

        protected static void Drawer_DifferentShapeError(HDProbeUI s, SerializedHDProbe d, Editor o)
        {
            var proxy = d.proxyVolumeReference.objectReferenceValue as ReflectionProxyVolumeComponent;
            if (proxy != null
                && (int)proxy.proxyVolume.shape != d.influenceVolume.shape.enumValueIndex
                && proxy.proxyVolume.shape != ProxyShape.Infinite)
            {
                EditorGUILayout.HelpBox(
                    proxyInfluenceShapeMismatchHelpBoxText,
                    MessageType.Error,
                    true
                    );
            }
        }
        
        static GUIStyle disabled;
        static void PropertyField(SerializedProperty prop, GUIContent content)
        {
            if(prop != null)
            {
                EditorGUILayout.PropertyField(prop, content);
            }
            else
            {
                if(disabled == null)
                {
                    disabled = new GUIStyle(GUI.skin.label);
                    disabled.onNormal.textColor *= 0.5f;
                }
                EditorGUILayout.LabelField(content, disabled);
            }
        }

        protected static void Drawer_SectionBakeButton(HDProbeUI s, SerializedHDProbe d, Editor o)
        {
            if (d.target is HDAdditionalReflectionData)
                EditorReflectionSystemGUI.DrawBakeButton((ReflectionProbeMode)d.mode.intValue, ((HDAdditionalReflectionData)d.target).reflectionProbe);
            else //PlanarReflectionProbe
                EditorReflectionSystemGUI.DrawBakeButton((ReflectionProbeMode)d.mode.intValue, d.target as PlanarReflectionProbe);
        }

        static void Drawer_SectionProbeModeRealtimeSettings(HDProbeUI s, SerializedHDProbe d, Editor o)
        {
            GUI.enabled = false;
            EditorGUILayout.PropertyField(d.refreshMode, _.GetContent("Refresh Mode"));
            GUI.enabled = true;
        }

        protected static void Drawer_SectionProxySettings(HDProbeUI s, SerializedHDProbe d, Editor o)
        {
            EditorGUILayout.PropertyField(d.proxyVolumeReference, _.GetContent("Reference"));

            if (d.proxyVolumeReference.objectReferenceValue != null)
            {
                var proxy = (ReflectionProxyVolumeComponent)d.proxyVolumeReference.objectReferenceValue;
                if ((int)proxy.proxyVolume.shape != d.influenceVolume.shape.enumValueIndex
                    && proxy.proxyVolume.shape != ProxyShape.Infinite)
                    EditorGUILayout.HelpBox(
                        proxyInfluenceShapeMismatchHelpBoxText,
                        MessageType.Error,
                        true
                        );
            }
            else
            {
                EditorGUILayout.HelpBox(
                        noProxyHelpBoxText,
                        MessageType.Info,
                        true
                        );
            }
        }

        protected static void Drawer_SectionCustomSettings(HDProbeUI s, SerializedHDProbe d, Editor o)
        {
            EditorGUILayout.PropertyField(d.weight, weightContent);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(d.multiplier, multiplierContent);
            if (EditorGUI.EndChangeCheck())
                d.multiplier.floatValue = Mathf.Max(0.0f, d.multiplier.floatValue);
        }

        static void Drawer_FieldCaptureType(HDProbeUI s, SerializedHDProbe d, Editor o)
        {
            GUI.enabled = false;
            EditorGUILayout.PropertyField(d.mode, fieldCaptureTypeContent);
            GUI.enabled = true;
        }



        protected enum ToolBar { Influence, Capture }
        protected ToolBar[] toolBars = new ToolBar[] { ToolBar.Influence, ToolBar.Capture };

        static readonly EditMode.SceneViewEditMode[] k_InfluenceToolbar_SceneViewEditModes =
        {
            EditBaseShape,
            EditInfluenceShape,
            EditInfluenceNormalShape,
        };

        static readonly EditMode.SceneViewEditMode[] k_CaptureToolbar_SceneViewEditModes =
        {
            EditCenter
        };

        protected static void Drawer_Toolbars(HDProbeUI s, SerializedHDProbe d, Editor o)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.changed = false;

            foreach(ToolBar toolBar in s.toolBars)
            {
                switch (toolBar)
                {
                    case ToolBar.Influence:
                        EditMode.DoInspectorToolbar(k_InfluenceToolbar_SceneViewEditModes, influenceToolbar_Contents, GetBoundsGetter(o), o);
                        break;
                    case ToolBar.Capture:
                        EditMode.DoInspectorToolbar(k_CaptureToolbar_SceneViewEditModes, captureToolbar_Contents, GetBoundsGetter(o), o);
                        break;
                }
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }


        static public void Drawer_InfluenceToolBarButton(int buttonIndex, Editor owner, params GUILayoutOption[] styles)
        {
            if (GUILayout.Button(influenceToolbar_Contents[buttonIndex], styles))
            {
                EditMode.ChangeEditMode(k_InfluenceToolbar_SceneViewEditModes[buttonIndex], GetBoundsGetter(owner)(), owner);
            }
        }

        static public void Drawer_CaptureToolBarButton(int buttonIndex, Editor owner, params GUILayoutOption[] styles)
        {
            if (GUILayout.Button(captureToolbar_Contents[buttonIndex], styles))
            {
                EditMode.ChangeEditMode(k_CaptureToolbar_SceneViewEditModes[buttonIndex], GetBoundsGetter(owner)(), owner);
            }
        }

        static Func<Bounds> GetBoundsGetter(Editor o)
        {
            return () =>
                {
                    var bounds = new Bounds();
                    foreach (Component targetObject in o.targets)
                    {
                        var rp = targetObject.transform;
                        var b = rp.position;
                        bounds.Encapsulate(b);
                    }
                    return bounds;
                };
        }

        static readonly KeyCode[] k_ShortCutKeys =
        {
            KeyCode.Alpha1,
            KeyCode.Alpha2,
            KeyCode.Alpha3,
        };

        public static void DoShortcutKey(Editor owner)
        {
            var evt = Event.current;
            if (evt.type != EventType.KeyDown || !evt.shift)
                return;

            for (var i = 0; i < k_ShortCutKeys.Length; ++i)
            {
                if (evt.keyCode == k_ShortCutKeys[i])
                {
                    var mode = EditMode.editMode == k_InfluenceToolbar_SceneViewEditModes[i]
                        ? EditMode.SceneViewEditMode.None
                        : k_InfluenceToolbar_SceneViewEditModes[i];
                    EditMode.ChangeEditMode(mode, GetBoundsGetter(owner)(), owner);
                    evt.Use();
                    break;
                }
            }
        }
    }
}
