using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Inspector.GraphicsSettingsInspectors;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;
using RenderingLayerMask = UnityEngine.RenderingLayerMask;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// A collection of utilities used by editor code of the HDRP.
    /// </summary>
    class HDEditorUtils
    {
        internal const string QualitySettingsSheetPath =
            @"Packages/com.unity.render-pipelines.high-definition/Editor/USS/QualitySettings";

        internal const string HDRPAssetBuildLabel = "HDRP:IncludeInBuild";

        internal static bool NeedsToBeIncludedInBuild(HDRenderPipelineAsset hdRenderPipelineAsset)
        {
            var labelList = AssetDatabase.GetLabels(hdRenderPipelineAsset);
            foreach (string item in labelList)
            {
                if (item == HDUtils.k_HdrpAssetBuildLabel)
                {
                    return true;
                }
            }

            return false;
        }

        private static (StyleSheet baseSkin, StyleSheet professionalSkin, StyleSheet personalSkin) LoadStyleSheets(string basePath)
            => (
                AssetDatabase.LoadAssetAtPath<StyleSheet>($"{basePath}.uss"),
                AssetDatabase.LoadAssetAtPath<StyleSheet>($"{basePath}Light.uss"),
                AssetDatabase.LoadAssetAtPath<StyleSheet>($"{basePath}Dark.uss")
            );

        internal static void AddStyleSheets(VisualElement element, string baseSkinPath)
        {
            (StyleSheet @base, StyleSheet personal, StyleSheet professional) = LoadStyleSheets(baseSkinPath);
            element.styleSheets.Add(@base);
            if (EditorGUIUtility.isProSkin)
            {
                if (professional != null && !professional.Equals(null))
                    element.styleSheets.Add(professional);
            }
            else
            {
                if (personal != null && !personal.Equals(null))
                    element.styleSheets.Add(personal);
            }
        }

        static readonly Action<SerializedProperty, GUIContent> k_DefaultDrawer = (p, l) => EditorGUILayout.PropertyField(p, l);


        internal static T LoadAsset<T>(string relativePath) where T : UnityEngine.Object
            => AssetDatabase.LoadAssetAtPath<T>(HDUtils.GetHDRenderPipelinePath() + relativePath);

        /// <summary>
        /// Reset the dedicated Keyword and Pass regarding the shader kind.
        /// Also re-init the drawers and set the material dirty for the engine.
        /// </summary>
        /// <param name="material">The material that nees to be setup</param>
        /// <returns>
        /// True: managed to do the operation.
        /// False: unknown shader used in material
        /// </returns>
        [Obsolete("Use HDShaderUtils.ResetMaterialKeywords instead")]
        public static bool ResetMaterialKeywords(Material material)
            => HDShaderUtils.ResetMaterialKeywords(material);

        static readonly GUIContent s_OverrideTooltip = EditorGUIUtility.TrTextContent("", "Override this setting in component.");

        internal static bool FlagToggle<TEnum>(TEnum v, SerializedProperty property)
            where TEnum : struct, IConvertible // restrict to ~enum
        {
            var intV = (int)(object)v;
            var isOn = (property.intValue & intV) != 0;
            var rect = ReserveAndGetFlagToggleRect();
            isOn = GUI.Toggle(rect, isOn, s_OverrideTooltip, CoreEditorStyles.smallTickbox);
            if (isOn)
                property.intValue |= intV;
            else
                property.intValue &= ~intV;

            return isOn;
        }

        internal static Rect ReserveAndGetFlagToggleRect()
        {
            var rect = GUILayoutUtility.GetRect(11, 17, GUILayout.ExpandWidth(false));
            rect.y += 4;
            return rect;
        }

        internal static bool IsAssetPath(string path)
        {
            var isPathRooted = Path.IsPathRooted(path);
            return isPathRooted && path.StartsWith(Application.dataPath)
                   || !isPathRooted && path.StartsWith("Assets");
        }

        // Copy texture from cache
        internal static bool CopyFileWithRetryOnUnauthorizedAccess(string s, string path)
        {
            UnauthorizedAccessException exception = null;
            for (var k = 0; k < 20; ++k)
            {
                try
                {
                    File.Copy(s, path, true);
                    exception = null;
                }
                catch (UnauthorizedAccessException e)
                {
                    exception = e;
                }
            }

            if (exception != null)
            {
                Debug.LogException(exception);
                // Abort the update, something else is preventing the copy
                return false;
            }

            return true;
        }

        internal static void PropertyFieldWithoutToggle<TEnum>(
            TEnum v, SerializedProperty property, GUIContent label, TEnum displayed,
            Action<SerializedProperty, GUIContent> drawer = null, int indent = 0
        )
            where TEnum : struct, IConvertible // restrict to ~enum
        {
            var intDisplayed = (int)(object)displayed;
            var intV = (int)(object)v;
            if ((intDisplayed & intV) == intV)
            {
                EditorGUILayout.BeginHorizontal();

                var i = EditorGUI.indentLevel;
                EditorGUI.indentLevel = i + indent;
                (drawer ?? k_DefaultDrawer)(property, label);
                EditorGUI.indentLevel = i;

                EditorGUILayout.EndHorizontal();
            }
        }

        internal static void DrawToolBarButton<TEnum>(
            TEnum button, Editor owner,
            Dictionary<TEnum, EditMode.SceneViewEditMode> toolbarMode,
            Dictionary<TEnum, GUIContent> toolbarContent,
            params GUILayoutOption[] options
        )
            where TEnum : struct, IConvertible
        {
            var intButton = (int)(object)button;
            bool enabled = toolbarMode[button] == EditMode.editMode;
            EditorGUI.BeginChangeCheck();
            enabled = GUILayout.Toggle(enabled, toolbarContent[button], EditorStyles.miniButton, options);
            if (EditorGUI.EndChangeCheck())
            {
                EditMode.SceneViewEditMode targetMode = EditMode.editMode == toolbarMode[button] ? EditMode.SceneViewEditMode.None : toolbarMode[button];
                EditMode.ChangeEditMode(targetMode, GetBoundsGetter(owner)(), owner);
            }
        }

        internal static Func<Bounds> GetBoundsGetter(Editor o)
        {
            return () =>
            {
                var bounds = new Bounds();
                var rp = ((Component)o.target).transform;
                var b = rp.position;
                bounds.Encapsulate(b);
                return bounds;
            };
        }

        /// <summary>
        /// Give a human readable string representing the inputed weight given in byte.
        /// </summary>
        /// <param name="weightInByte">The weigth in byte</param>
        /// <returns>Human readable weight</returns>
        internal static string HumanizeWeight(long weightInByte)
        {
            if (weightInByte < 500)
            {
                return weightInByte + " B";
            }
            else if (weightInByte < 500000L)
            {
                float res = weightInByte / 1000f;
                return res.ToString("n2") + " KB";
            }
            else if (weightInByte < 500000000L)
            {
                float res = weightInByte / 1000000f;
                return res.ToString("n2") + " MB";
            }
            else
            {
                float res = weightInByte / 1000000000f;
                return res.ToString("n2") + " GB";
            }
        }

        /// <summary>
        /// Should be placed between BeginProperty / EndProperty
        /// </summary>
        internal static uint DrawRenderingLayerMask(Rect rect, uint renderingLayer, GUIContent label = null, bool allowHelpBox = true)
        {
            var value = EditorGUI.RenderingLayerMaskField(rect, label ?? GUIContent.none, renderingLayer);
            return value;
        }

        internal static void DrawRenderingLayerMask(Rect rect, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(rect, label, property);

            EditorGUI.BeginChangeCheck();
            var renderingLayer = DrawRenderingLayerMask(rect, property.uintValue, label);
            if (EditorGUI.EndChangeCheck())
            {
                if(property.numericType == SerializedPropertyNumericType.UInt32)
                    property.uintValue = renderingLayer;
                else
                    property.intValue = unchecked((int)renderingLayer);
            }

            EditorGUI.EndProperty();
        }

        internal static void DrawRenderingLayerMask(SerializedProperty property, GUIContent style)
        {
            Rect rect = EditorGUILayout.GetControlRect(true);
            DrawRenderingLayerMask(rect, property, style);
        }

        // IsPreset is an internal API - lets reuse the usable part of this function
        // 93 is a "magic number" and does not represent a combination of other flags here
        internal static bool IsPresetEditor(UnityEditor.Editor editor)
        {
            return (int)((editor.target as Component).gameObject.hideFlags) == 93;
        }

        internal static void QualitySettingsHelpBox(string message, MessageType type, HDRenderPipelineUI.ExpandableGroup uiGroupSection, string propertyPath)
        {
            CoreEditorUtils.DrawFixMeBox(message, type, "Open", () =>
            {
                SettingsService.OpenProjectSettings("Project/Quality/HDRP");
                HDRenderPipelineUI.Inspector.Expand((int)uiGroupSection);
                CoreEditorUtils.Highlight("Project Settings", propertyPath, HighlightSearchMode.Identifier);
                GUIUtility.ExitGUI();
            });
        }

        internal static void QualitySettingsHelpBox<TEnum>(string message, MessageType type, HDRenderPipelineUI.ExpandableGroup uiGroupSection, TEnum uiSection, string propertyPath)
            where TEnum : struct, IConvertible
        {
            QualitySettingsHelpBoxForReflection(message, type, uiGroupSection, uiSection.ToInt32(System.Globalization.CultureInfo.InvariantCulture), propertyPath);
        }

        internal static void QualitySettingsHelpBoxForReflection(string message, MessageType type, HDRenderPipelineUI.ExpandableGroup uiGroupSection, int uiSection, string propertyPath)
        {
            CoreEditorUtils.DrawFixMeBox(message, type, "Open", () =>
            {
                SettingsService.OpenProjectSettings("Project/Quality/HDRP");
                HDRenderPipelineUI.SubInspectors[uiGroupSection].Expand(uiSection == -1 ? (int)uiGroupSection : uiSection);

                CoreEditorUtils.Highlight("Project Settings", propertyPath, HighlightSearchMode.Identifier);
                GUIUtility.ExitGUI();
            });
        }

        internal static void GlobalSettingsHelpBox<TGraphicsSettings>(string message, MessageType type)
            where TGraphicsSettings: IRenderPipelineGraphicsSettings
        {
            CoreEditorUtils.DrawFixMeBox(message, type, "Open", () =>
            {
                GraphicsSettingsInspectorUtility.OpenAndScrollTo<TGraphicsSettings>();
            });
        }

        internal static void GlobalSettingsHelpBox(string message, MessageType type, FrameSettingsField field, string displayName)
        {
            CoreEditorUtils.DrawFixMeBox(message, type, "Open", () =>
            {
                var attribute = FrameSettingsExtractedDatas.GetFieldAttribute(field);

                GraphicsSettingsInspectorUtility.OpenAndScrollTo<RenderingPathFrameSettings, FrameSettingsArea.LineField>(line =>
                {
                    if (line.name != $"line-field-{field}")
                        return false;

                    FrameSettingsPropertyDrawer.SetExpended(FrameSettingsRenderType.Camera.ToString(), attribute.group, true);
                    return true;
                });
            });
        }

        // This is used through reflection by inspector in srp core
        static bool DataDrivenLensFlareHelpBox()
        {
            if (!HDRenderPipeline.currentAsset?.currentPlatformRenderPipelineSettings.supportDataDrivenLensFlare ?? false)
            {
                EditorGUILayout.Space();
                HDEditorUtils.QualitySettingsHelpBox("The current HDRP Asset does not support Data Driven Lens Flare.", MessageType.Error,
                    HDRenderPipelineUI.ExpandableGroup.PostProcess, HDRenderPipelineUI.ExpandablePostProcess.LensFlare, "m_RenderPipelineSettings.supportDataDrivenLensFlare");
                return false;
            }

            HDEditorUtils.EnsureFrameSetting(FrameSettingsField.LensFlareDataDriven);
            return true;
        }

        static void OpenRenderingDebugger(string panelName)
        {
            EditorApplication.ExecuteMenuItem("Window/Analysis/Rendering Debugger");

            if (panelName != null)
            {
                var manager = DebugManager.instance;
                manager.RequestEditorWindowPanelIndex(manager.FindPanelIndex(panelName));
            }
        }

        static void HighlightInDebugger(Camera camera, FrameSettingsField field, string displayName)
        {
            OpenRenderingDebugger(camera.name);

            // Doesn't work for some reason
            //CoreEditorUtils.Highlight("Rendering Debugger", displayName, HighlightSearchMode.Auto);
            //GUIUtility.ExitGUI();
        }

        static IEnumerable<Camera> GetAllCameras()
        {
            foreach (SceneView sceneView in SceneView.sceneViews)
                yield return sceneView.camera;
            foreach (Camera camera in Camera.allCameras)
                if (camera.cameraType == CameraType.Game)
                    yield return camera;
        }
        
        static IEnumerable<(Camera camera, FrameSettings @default, IFrameSettingsHistoryContainer historyContainer)> SelectFrameSettingsStages(IEnumerable<Camera> cameras)
        {
            var supportedFeatures = HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings;
            var defaultSettings = GraphicsSettings.GetRenderPipelineSettings<RenderingPathFrameSettings>().GetDefaultFrameSettings(FrameSettingsRenderType.Camera);

            foreach (var camera in cameras)
            {
                var additionalCameraData = HDUtils.TryGetAdditionalCameraDataOrDefault(camera);
                var historyContainer = camera.cameraType == CameraType.SceneView ? FrameSettingsHistory.sceneViewFrameSettingsContainer : additionalCameraData;

                FrameSettings dummy = default;
                FrameSettingsHistory.AggregateFrameSettings(ref dummy, camera, historyContainer, ref defaultSettings, supportedFeatures);
                yield return (camera, defaultSettings, historyContainer);
            }
        }
        
        static void FrameSettingsHelpBox(Camera camera, FrameSettingsField field, FrameSettings @default, IFrameSettingsHistoryContainer historyContainer)
        {
            FrameSettingsHistory history = historyContainer.frameSettingsHistory;
            bool finalValue = history.debug.IsEnabled(field); 
            if (finalValue) return; //must be false to call this method

            bool defaultValue = @default.IsEnabled(field);
            bool cameraOverrideState = historyContainer.hasCustomFrameSettings && history.customMask.mask[(uint)field];
            bool cameraOverridenValue = history.overridden.IsEnabled(field);
            bool cameraSanitizedValue = history.sanitazed.IsEnabled(field);

            var attribute = FrameSettingsExtractedDatas.GetFieldAttribute(field);
            bool dependenciesSanitizedValueOk = attribute.dependencies.All(fs => attribute.IsNegativeDependency(fs) ? !history.sanitazed.IsEnabled(fs) : history.sanitazed.IsEnabled(fs));

            bool disabledByDefault = !defaultValue && !cameraOverrideState;
            bool disabledByCameraOverride = cameraOverrideState && !cameraOverridenValue;
            
            var textBase = $"The FrameSetting required to render this effect in the {(camera.cameraType == CameraType.SceneView ? "Scene" : "Game")} view (by {camera.name}) ";

            if (disabledByDefault)
                GlobalSettingsHelpBox(textBase + "is disabled in the HDRP Global Settings.", MessageType.Warning, field, attribute.displayedName);
            else if (disabledByCameraOverride)
                CoreEditorUtils.DrawFixMeBox(textBase + $"is disabled on the Camera.", MessageType.Warning, "Open", () => EditorUtility.OpenPropertyEditor(camera));
            else if (!dependenciesSanitizedValueOk)
                GlobalSettingsHelpBox(textBase + "depends on a disabled FrameSetting.", MessageType.Warning, field, attribute.displayedName);
            else if (!finalValue)
                CoreEditorUtils.DrawFixMeBox(textBase + "is disabled in the Rendering Debugger.", MessageType.Warning, "Open", () => HighlightInDebugger(camera, field, attribute.displayedName));
        }

        internal static bool EnsureFrameSetting(FrameSettingsField field)
        {
            foreach ((Camera camera, FrameSettings @default, IFrameSettingsHistoryContainer historyContainer) in SelectFrameSettingsStages(GetAllCameras()))
            {
                if (!historyContainer.frameSettingsHistory.debug.IsEnabled(field))
                {
                    FrameSettingsHelpBox(camera, field, @default, historyContainer);
                    EditorGUILayout.Space();
                    return false;
                }
            }

            return true;
        }
        
        static IEnumerable<(Camera camera, T component)> SelectVolumeComponent<T>(IEnumerable<Camera> cameras) where T : VolumeComponent
        {
            // Wait for volume system to be initialized
            if (VolumeManager.instance.baseComponentTypeArray == null)
                yield break;

            foreach (var camera in GetAllCameras())
            {
                if (!HDCamera.TryGet(camera, out var hdCamera))
                    continue;

                T component = hdCamera.volumeStack.GetComponent<T>();
                if (component == null)
                    continue;

                yield return (camera, component);
            }
        }

        internal static bool EnsureVolume<T>(Func<T, string> volumeValidator) where T : VolumeComponent
        {
            foreach ((Camera camera, T component) in SelectVolumeComponent<T>(GetAllCameras()))
            {
                var errorString = volumeValidator(component);
                if (!string.IsNullOrEmpty(errorString))
                {
                    EditorGUILayout.HelpBox(errorString, MessageType.Warning);
                    EditorGUILayout.Space();
                    return false;
                }
            }

            return true;
        }
    }

    // Due to a UI bug/limitation, we have to do it this way to support bold labels
    internal class BoldLabelScope : GUI.Scope
    {
        FontStyle origFontStyle;

        public BoldLabelScope()
        {
            origFontStyle = EditorStyles.label.fontStyle;
            EditorStyles.label.fontStyle = FontStyle.Bold;
        }

        protected override void CloseScope()
        {
            EditorStyles.label.fontStyle = origFontStyle;
        }
    }
}
