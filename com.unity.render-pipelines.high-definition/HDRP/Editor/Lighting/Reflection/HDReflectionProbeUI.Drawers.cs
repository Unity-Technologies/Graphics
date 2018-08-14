using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using CED = CoreEditorDrawer<HDProbeUI, SerializedHDProbe>;

    internal partial class HDReflectionProbeUI
    {
        public static readonly CED.IDrawer[] Inspector;

        static HDReflectionProbeUI()
        {
            Inspector = new[]
            {
                SectionPrimarySettings,
                ProxyVolumeSettings,
                CED.Select(
                        (s, d, o) => s.influenceVolume,
                        (s, d, o) => d.influenceVolume,
                        InfluenceVolumeUI.SectionFoldoutShape
                        ),
                CED.Action((s, d, o) => Drawer_DifferentShapeError(s, d, o)),
                SectionCaptureSettings,
                SectionFoldoutAdditionalSettings,
                CED.Action((s, d, o) => Drawer_SectionBakeButton(s, d, o))
            };
        }

        static readonly CED.IDrawer SectionPrimarySettings = CED.Group(
                CED.Action((s, d, o) => Drawer_Toolbars(s, d, o)),
                CED.space,
                CED.Action(Drawer_ReflectionProbeMode),
                CED.space,
                CED.FadeGroup((s, p, o, i) => s.IsSectionExpandedReflectionProbeMode((ReflectionProbeMode)i),
                    FadeOption.Indent,
                    CED.noop,                                                       // Baked
                    CED.Action((s, d, o) => Drawer_ModeSettingsRealtime(s, d, o)),  // Realtime
                    CED.Action((s, d, o) => Drawer_ModeSettingsCustom(s, d, o))     // Custom
                    )
                );

        static readonly CED.IDrawer SectionCaptureSettings = CED.FoldoutGroup(
                captureSettingsHeader,
                (s, p, o) => s.isSectionExpandedCaptureSettings,
                FoldoutOption.Indent,
                CED.Action(Drawer_CaptureSettings)
                );
        
        static void Drawer_CaptureSettings(HDProbeUI s, SerializedHDProbe p, Editor owner)
        {
            var renderPipelineAsset = (HDRenderPipelineAsset)GraphicsSettings.renderPipelineAsset;
            p.resolution.intValue = (int)renderPipelineAsset.GetRenderPipelineSettings().lightLoopSettings.reflectionCubemapSize;
            EditorGUILayout.LabelField(resolutionContent, CoreEditorUtils.GetContent(p.resolution.intValue.ToString()));

            EditorGUILayout.PropertyField(p.shadowDistance, shadowDistanceContent);
            EditorGUILayout.PropertyField(p.cullingMask, cullingMaskContent);
            EditorGUILayout.PropertyField(p.useOcclusionCulling, useOcclusionCullingContent);
            EditorGUILayout.PropertyField(p.nearClip, nearClipCullingContent);
            EditorGUILayout.PropertyField(p.farClip, farClipCullingContent);
        }

        static readonly GUIContent[] k_Content_ReflectionProbeMode = { new GUIContent("Baked"), new GUIContent("Custom"), new GUIContent("Realtime") };
        static readonly int[] k_Content_ReflectionProbeModeValues = { (int)ReflectionProbeMode.Baked, (int)ReflectionProbeMode.Custom, (int)ReflectionProbeMode.Realtime };
        static void Drawer_ReflectionProbeMode(HDProbeUI s, SerializedHDProbe p, Editor owner)
        {
            HDReflectionProbeUI ui = ((HDReflectionProbeEditor)owner).m_UIState;
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = p.mode.hasMultipleDifferentValues;
            EditorGUILayout.IntPopup(p.mode, k_Content_ReflectionProbeMode, k_Content_ReflectionProbeModeValues, CoreEditorUtils.GetContent("Type|'Baked Cubemap' uses the 'Auto Baking' mode from the Lighting window. If it is enabled then baking is automatic otherwise manual bake is needed (use the bake button below). \n'Custom' can be used if a custom cubemap is wanted. \n'Realtime' can be used to dynamically re-render the cubemap during runtime (via scripting)."));
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                ui.SetModeTarget(p.mode.intValue);
                p.Apply();
            }
        }

        #region Mode Specific Settings
        static void Drawer_ModeSettingsCustom(HDProbeUI s, SerializedHDProbe p, Editor owner)
        {
            SerializedHDReflectionProbe probe = (SerializedHDReflectionProbe)p;
            EditorGUILayout.PropertyField(probe.renderDynamicObjects, CoreEditorUtils.GetContent("Dynamic Objects|If enabled dynamic objects are also rendered into the cubemap"));

            EditorGUI.showMixedValue = probe.customBakedTexture.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            var customBakedTexture = EditorGUILayout.ObjectField(CoreEditorUtils.GetContent("Cubemap"), probe.customBakedTexture.objectReferenceValue, typeof(Cubemap), false);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
                probe.customBakedTexture.objectReferenceValue = customBakedTexture;
        }

        static void Drawer_ModeSettingsRealtime(HDProbeUI s, SerializedHDProbe p, Editor owner)
        {
            //SerializedHDReflectionProbe probe = (SerializedHDReflectionProbe)p;
            //EditorGUILayout.PropertyField(p.refreshMode, CoreEditorUtils.GetContent("Refresh Mode|Controls how this probe refreshes in the Player"));
            //EditorGUILayout.PropertyField(probe.timeSlicingMode, CoreEditorUtils.GetContent("Time Slicing|If enabled this probe will update over several frames, to help reduce the impact on the frame rate"));
        }

        #endregion

        static MethodInfo k_EditorGUI_ButtonWithDropdownList = typeof(EditorGUI).GetMethod("ButtonWithDropdownList", BindingFlags.Static | BindingFlags.NonPublic, null, CallingConventions.Any, new[] { typeof(GUIContent), typeof(string[]), typeof(GenericMenu.MenuFunction2), typeof(GUILayoutOption[]) }, new ParameterModifier[0]);
        static bool ButtonWithDropdownList(GUIContent content, string[] buttonNames, GenericMenu.MenuFunction2 callback, params GUILayoutOption[] options)
        {
            return (bool)k_EditorGUI_ButtonWithDropdownList.Invoke(null, new object[] { content, buttonNames, callback, options });
        }
    }
}
