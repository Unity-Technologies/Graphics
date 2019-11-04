using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEditor.Experimental.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    static partial class HDProbeUI
    {
        [Flags]
        internal enum ToolBar
        {
            None = 0,
            InfluenceShape = 1 << 0,
            Blend = 1 << 1,
            NormalBlend = 1 << 2,
            CapturePosition = 1 << 3,
            MirrorPosition = 1 << 4,
            MirrorRotation = 1 << 5
        }

        internal interface IProbeUISettingsProvider
        {
            ProbeSettingsOverride displayedCaptureSettings { get; }
            ProbeSettingsOverride displayedAdvancedCaptureSettings { get; }
            ProbeSettingsOverride overrideableCaptureSettings { get; }
            ProbeSettingsOverride overrideableAdvancedCaptureSettings { get; }
            ProbeSettingsOverride displayedCustomSettings { get; }
            ProbeSettingsOverride displayedAdvancedCustomSettings { get; }
            ProbeSettingsOverride overrideableCustomSettings { get; }
            ProbeSettingsOverride overrideableAdvancedCustomSettings { get; }
            Type customTextureType { get; }
            ToolBar[] toolbars { get; }
            Dictionary<KeyCode, ToolBar> shortcuts { get; }
        }

        // Constants
        const EditMode.SceneViewEditMode EditBaseShape = (EditMode.SceneViewEditMode)100;
        const EditMode.SceneViewEditMode EditInfluenceShape = (EditMode.SceneViewEditMode)101;
        const EditMode.SceneViewEditMode EditInfluenceNormalShape = (EditMode.SceneViewEditMode)102;
        const EditMode.SceneViewEditMode EditCapturePosition = (EditMode.SceneViewEditMode)103;
        const EditMode.SceneViewEditMode EditMirrorPosition = (EditMode.SceneViewEditMode)104;
        const EditMode.SceneViewEditMode EditMirrorRotation = (EditMode.SceneViewEditMode)105;
        //Note: EditMode.SceneViewEditMode.ReflectionProbeOrigin is still used
        //by legacy reflection probe and have its own mecanism that we don't want

        static readonly Dictionary<ToolBar, EditMode.SceneViewEditMode> k_ToolbarMode = new Dictionary<ToolBar, EditMode.SceneViewEditMode>
        {
            { ToolBar.InfluenceShape, EditBaseShape },
            { ToolBar.Blend, EditInfluenceShape },
            { ToolBar.NormalBlend, EditInfluenceNormalShape },
            { ToolBar.CapturePosition, EditCapturePosition },
            { ToolBar.MirrorPosition, EditMirrorPosition },
            { ToolBar.MirrorRotation, EditMirrorRotation }
        };

        // Probe Setting Mode cache
        static readonly GUIContent[] k_ModeContents = { new GUIContent("Baked"), new GUIContent("Custom"), new GUIContent("Realtime") };
        static readonly int[] k_ModeValues = { (int)ProbeSettings.Mode.Baked, (int)ProbeSettings.Mode.Custom, (int)ProbeSettings.Mode.Realtime };

        protected internal struct Drawer<TProvider>
            where TProvider : struct, IProbeUISettingsProvider, InfluenceVolumeUI.IInfluenceUISettingsProvider
        {
            // Toolbar content cache
            static readonly EditMode.SceneViewEditMode[][] k_ListModes;
            static readonly GUIContent[][] k_ListContent;

            static Drawer()
            {
                var provider = new TProvider();

                // Build toolbar content cache
                var toolbars = provider.toolbars;
                k_ListContent = new GUIContent[toolbars.Length][];
                k_ListModes = new EditMode.SceneViewEditMode[toolbars.Length][];

                var listMode = new List<EditMode.SceneViewEditMode>();
                var listContent = new List<GUIContent>();
                for (int i = 0; i < toolbars.Length; ++i)
                {
                    listMode.Clear();
                    listContent.Clear();

                    var toolBar = toolbars[i];
                    for (int j = 0; j < sizeof(int) * 8; ++j)
                    {
                        var toolbarJ = (ToolBar)(1 << j);
                        if ((toolBar & toolbarJ) > 0)
                        {
                            listMode.Add(k_ToolbarMode[toolbarJ]);
                            listContent.Add(k_ToolbarContents[toolbarJ]);
                        }
                    }
                    k_ListContent[i] = listContent.ToArray();
                    k_ListModes[i] = listMode.ToArray();
                }

            }

            // Tool bars
            public static void DrawToolbars(SerializedHDProbe serialized, Editor owner)
            {
                var provider = new TProvider();

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUI.changed = false;

                for (int i = 0; i < k_ListModes.Length; ++i)
                    EditMode.DoInspectorToolbar(k_ListModes[i], k_ListContent[i], HDEditorUtils.GetBoundsGetter(owner), owner);

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            public static void DoToolbarShortcutKey(Editor owner)
            {
                var provider = new TProvider();
                var toolbars = provider.toolbars;
                var shortcuts = provider.shortcuts;

                var evt = Event.current;
                if (evt.type != EventType.KeyDown || !evt.shift)
                    return;

                if (shortcuts.TryGetValue(evt.keyCode, out ToolBar toolbar))
                {
                    bool used = false;
                    foreach (ToolBar t in toolbars)
                    {
                        if ((t & toolbar) > 0)
                        {
                            used = true;
                            break;
                        }
                    }
                    if (!used)
                        return;

                    var targetMode = k_ToolbarMode[toolbar];
                    var mode = EditMode.editMode == targetMode ? EditMode.SceneViewEditMode.None : targetMode;
                    EditorApplication.delayCall += () => EditMode.ChangeEditMode(mode, HDEditorUtils.GetBoundsGetter(owner)(), owner);
                    evt.Use();
                }
            }

            // Drawers
            public static void DrawPrimarySettings(SerializedHDProbe serialized, Editor owner)
            {
                const string modeTooltip = "'Baked' uses the 'Auto Baking' mode from the Lighting window. " +
                    "If it is enabled then baking is automatic otherwise manual bake is needed (use the bake button below). \n" +
                    "'Custom' can be used if a custom capture is wanted. \n" +
                    "'Realtime' can be used to dynamically re-render the capture during runtime (every frame).";

                var provider = new TProvider();

#if !ENABLE_BAKED_PLANAR
                if (serialized is SerializedPlanarReflectionProbe)
                {
                    serialized.probeSettings.mode.intValue = (int)ProbeSettings.Mode.Realtime;
                }
                else
                {
#endif

                // Probe Mode
                EditorGUI.showMixedValue = serialized.probeSettings.mode.hasMultipleDifferentValues;
                EditorGUILayout.IntPopup(serialized.probeSettings.mode, k_ModeContents, k_ModeValues, EditorGUIUtility.TrTextContent("Type", modeTooltip));
                EditorGUI.showMixedValue = false;

#if !ENABLE_BAKED_PLANAR
                }
#endif

                switch ((ProbeSettings.Mode)serialized.probeSettings.mode.intValue)
                {
                    case ProbeSettings.Mode.Realtime:
                        {
                            EditorGUI.showMixedValue = serialized.probeSettings.realtimeMode.hasMultipleDifferentValues;
                            EditorGUILayout.PropertyField(serialized.probeSettings.realtimeMode);
                            EditorGUI.showMixedValue = false;
                            break;
                        }
                    case ProbeSettings.Mode.Custom:
                        {
                            EditorGUI.showMixedValue = serialized.customTexture.hasMultipleDifferentValues;
                            EditorGUI.BeginChangeCheck();
                            var customTexture = EditorGUILayout.ObjectField(
                                EditorGUIUtility.TrTextContent("Texture"), serialized.customTexture.objectReferenceValue, provider.customTextureType, false
                            );
                            EditorGUI.showMixedValue = false;
                            if (EditorGUI.EndChangeCheck())
                                serialized.customTexture.objectReferenceValue = customTexture;
                            break;
                        }
                }
            }

            public static void DrawCaptureSettings(SerializedHDProbe serialized, Editor owner)
            {
                var provider = new TProvider();
                ProbeSettingsUI.Draw(
                    serialized.probeSettings, owner,
                    serialized.probeSettingsOverride,
                    provider.displayedCaptureSettings, provider.overrideableCaptureSettings
                );
            }

            public static void DrawAdvancedCaptureSettings(SerializedHDProbe serialized, Editor owner)
            {
                var provider = new TProvider();
                ProbeSettingsUI.Draw(
                    serialized.probeSettings, owner,
                    serialized.probeSettingsOverride,
                    provider.displayedAdvancedCaptureSettings, provider.overrideableAdvancedCaptureSettings
                );
            }

            public static void DrawCustomSettings(SerializedHDProbe serialized, Editor owner)
            {
                var provider = new TProvider();
                ProbeSettingsUI.Draw(
                    serialized.probeSettings, owner,
                    serialized.probeSettingsOverride,
                    provider.displayedCustomSettings, provider.overrideableCustomSettings
                );
            }

            public static void DrawAdvancedCustomSettings(SerializedHDProbe serialized, Editor owner)
            {
                var provider = new TProvider();
                ProbeSettingsUI.Draw(
                    serialized.probeSettings, owner,
                    serialized.probeSettingsOverride,
                    provider.displayedAdvancedCustomSettings, provider.overrideableAdvancedCustomSettings
                );
            }

            public static void DrawInfluenceSettings(SerializedHDProbe serialized, Editor owner)
            {
                var provider = new TProvider();
                InfluenceVolumeUI.Draw<TProvider>(serialized.probeSettings.influence, owner);
            }

            public static void DrawProjectionSettings(SerializedHDProbe serialized, Editor owner)
            {
                EditorGUILayout.PropertyField(serialized.proxyVolume, k_ProxyVolumeContent);

                if (serialized.target.proxyVolume == null)
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(serialized.probeSettings.proxyUseInfluenceVolumeAsProxyVolume);
                    if (EditorGUI.EndChangeCheck())
                        serialized.Apply();
                }

                if (serialized.proxyVolume.objectReferenceValue != null)
                {
                    var proxy = (ReflectionProxyVolumeComponent)serialized.proxyVolume.objectReferenceValue;
                    if (proxy.proxyVolume.shape != serialized.probeSettings.influence.shape.GetEnumValue<ProxyShape>()
                        && proxy.proxyVolume.shape != ProxyShape.Infinite)
                        EditorGUILayout.HelpBox(
                            k_ProxyInfluenceShapeMismatchHelpBoxText,
                            MessageType.Error,
                            true
                            );
                }
                else
                {
                    EditorGUILayout.HelpBox(
                            serialized.probeSettings.proxyUseInfluenceVolumeAsProxyVolume.boolValue ? k_NoProxyHelpBoxText : k_NoProxyInfiniteHelpBoxText,
                            MessageType.Info,
                            true
                            );
                }
            }

            static readonly string[] k_BakeCustomOptionText = { "Bake as new Cubemap..." };
            static readonly string[] k_BakeButtonsText = { "Bake All Reflection Probes" };
            public static void DrawBakeButton(SerializedHDProbe serialized, Editor owner)
            {
                // Disable baking of multiple probes with different modes
                if (serialized.probeSettings.mode.hasMultipleDifferentValues)
                {
                    EditorGUILayout.HelpBox(
                        "Baking is not possible when selecting probe with different modes",
                        MessageType.Info
                    );
                    return;
                }

                // Check if current mode support baking
                var mode = (ProbeSettings.Mode)serialized.probeSettings.mode.intValue;
                var doesModeSupportBaking = mode == ProbeSettings.Mode.Custom || mode == ProbeSettings.Mode.Baked;
                if (!doesModeSupportBaking)
                    return;

                // Check if all scene are saved to a file (requirement to bake probes)
                foreach (var target in serialized.serializedObject.targetObjects)
                {
                    var comp = (Component)target;
                    var go = comp.gameObject;
                    var scene = go.scene;
                    if (string.IsNullOrEmpty(scene.path))
                    {
                        EditorGUILayout.HelpBox(
                            "Baking is possible only if all open scenes are saved on disk. " +
                            "Please save the scenes before baking.",
                            MessageType.Info
                        );
                        return;
                    }
                }

                switch (mode)
                {
                    case ProbeSettings.Mode.Custom:
                        {
                            if (ButtonWithDropdownList(
                                EditorGUIUtility.TrTextContent(
                                    "Bake", "Bakes Probe's texture, overwriting the existing texture asset (if any)."
                                ),
                                k_BakeCustomOptionText,
                                data =>
                                {
                                    switch ((int)data)
                                    {
                                        case 0:
                                            RenderInCustomAsset(serialized.target, false);
                                            break;
                                    }
                                }))
                            {
                                RenderInCustomAsset(serialized.target, true);
                            }
                            break;
                        }
                    case ProbeSettings.Mode.Baked:
                        {
                            if (UnityEditor.Lightmapping.giWorkflowMode
                                != UnityEditor.Lightmapping.GIWorkflowMode.OnDemand)
                            {
                                EditorGUILayout.HelpBox("Baking of this probe is automatic because this probe's type is 'Baked' and the Lighting window is using 'Auto Baking'. The texture created is stored in the GI cache.", MessageType.Info);
                                break;
                            }

                            GUI.enabled = serialized.target.enabled;

                            // Bake button in non-continous mode
                            if (ButtonWithDropdownList(
                                    EditorGUIUtility.TrTextContent("Bake"),
                                    k_BakeButtonsText,
                                    data =>
                                    {
                                        if ((int)data == 0)
                                        {
                                            var system = ScriptableBakedReflectionSystemSettings.system;
                                            system.BakeAllReflectionProbes();
                                        }
                                    },
                                    GUILayout.ExpandWidth(true)))
                            {
                                HDBakedReflectionSystem.BakeProbes(serialized.serializedObject.targetObjects.OfType<HDProbe>().ToArray());
                                GUIUtility.ExitGUI();
                            }

                            GUI.enabled = true;
                            break;
                        }
                    case ProbeSettings.Mode.Realtime:
                        break;
                    default: throw new ArgumentOutOfRangeException();
                }
            }

            static MethodInfo k_EditorGUI_ButtonWithDropdownList = typeof(EditorGUI).GetMethod("ButtonWithDropdownList", BindingFlags.Static | BindingFlags.NonPublic, null, CallingConventions.Any, new[] { typeof(GUIContent), typeof(string[]), typeof(GenericMenu.MenuFunction2), typeof(GUILayoutOption[]) }, new ParameterModifier[0]);
            static bool ButtonWithDropdownList(GUIContent content, string[] buttonNames, GenericMenu.MenuFunction2 callback, params GUILayoutOption[] options)
            {
                return (bool)k_EditorGUI_ButtonWithDropdownList.Invoke(null, new object[] { content, buttonNames, callback, options });
            }

            static void RenderInCustomAsset(HDProbe probe, bool useExistingCustomAsset)
            {
                var provider = new TProvider();

                string assetPath = null;
                if (useExistingCustomAsset && probe.customTexture != null && !probe.customTexture.Equals(null))
                    assetPath = AssetDatabase.GetAssetPath(probe.customTexture);

                if (string.IsNullOrEmpty(assetPath))
                {
                    assetPath = EditorUtility.SaveFilePanelInProject(
                        "Save custom capture",
                        probe.name, "exr",
                        "Save custom capture");
                }

                if (!string.IsNullOrEmpty(assetPath))
                {
                    var target = (RenderTexture)HDProbeSystem.CreateRenderTargetForMode(
                        probe, ProbeSettings.Mode.Custom
                    );


                    HDBakedReflectionSystem.RenderAndWriteToFile(
                        probe, assetPath, target, null,
                        out var cameraSettings, out var cameraPositionSettings
                    );
                    AssetDatabase.ImportAsset(assetPath);
                    HDBakedReflectionSystem.ImportAssetAt(probe, assetPath);
                    CoreUtils.Destroy(target);

                    var assetTarget = AssetDatabase.LoadAssetAtPath<Texture>(assetPath);
                    probe.SetTexture(ProbeSettings.Mode.Custom, assetTarget);
                    probe.SetRenderData(ProbeSettings.Mode.Custom, new HDProbe.RenderData(cameraSettings, cameraPositionSettings));
                    EditorUtility.SetDirty(probe);
                }
            }
        }

        static internal void Drawer_DifferentShapeError(SerializedHDProbe serialized, Editor owner)
        {
            var proxy = serialized.proxyVolume.objectReferenceValue as ReflectionProxyVolumeComponent;
            if (proxy != null
                && proxy.proxyVolume.shape != serialized.probeSettings.influence.shape.GetEnumValue<ProxyShape>()
                && proxy.proxyVolume.shape != ProxyShape.Infinite)
            {
                EditorGUILayout.HelpBox(
                    k_ProxyInfluenceShapeMismatchHelpBoxText,
                    MessageType.Error,
                    true
                    );
            }
        }

        static internal void Drawer_ToolBarButton(
            ToolBar button, Editor owner,
            params GUILayoutOption[] options
        )
            => HDEditorUtils.DrawToolBarButton(button, owner, k_ToolbarMode, k_ToolbarContents, options);
    }
}
