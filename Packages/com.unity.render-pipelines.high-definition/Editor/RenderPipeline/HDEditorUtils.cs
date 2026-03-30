using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.Inspector.GraphicsSettingsInspectors;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;

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
        [Obsolete("Use HDShaderUtils.ResetMaterialKeywords instead. #from(2021.1)")]
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
                DebugManager.instance.RequestEditorWindowPanel(panelName);
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

            // If the setting is enabled in the frame settings but is disabled in the HDRP Asset (cameraSanitizedValue), it means the feature is disabled and we should not display anything.
            bool disabledbySanitized = (cameraOverrideState ? cameraOverridenValue : defaultValue) && !cameraSanitizedValue;

            var textBase = $"The Frame Setting required to render this effect in the {(camera.cameraType == CameraType.SceneView ? "Scene" : "Game")} view ";

            if (disabledByDefault)
                GlobalSettingsHelpBox(textBase + "is disabled in the HDRP Default Frame Settings.", MessageType.Warning, field, attribute.displayedName);
            else if (disabledByCameraOverride)
                CoreEditorUtils.DrawFixMeBox(textBase + $"is disabled in the {camera.name}'s Custom Frame Settings.", MessageType.Warning, "Open", () => EditorUtility.OpenPropertyEditor(camera));
            else if (!dependenciesSanitizedValueOk)
            {
                if(cameraOverrideState)
                    CoreEditorUtils.DrawFixMeBox(textBase + $"depends on a disabled Frame Setting parent in the {camera.name} Custom Frame Settings.", MessageType.Warning, "Open", () => EditorUtility.OpenPropertyEditor(camera));
                else
                    GlobalSettingsHelpBox(textBase + "depends on a disabled Frame Setting parent in the HDRP Default Frame Settings.", MessageType.Warning, field, attribute.displayedName);

            }
            else if (!finalValue && !disabledbySanitized)
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
            if (!VolumeManager.instance.isInitialized)
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

        /// <summary>
        /// Gets the resolved default state for a volume component, which combines global and quality default profiles.
        /// Use this to determine what values will be used for non-overridden parameters.
        /// </summary>
        /// <typeparam name="T">The type of volume component to retrieve</typeparam>
        /// <returns>The volume component with resolved default state, or null if not initialized</returns>
        internal static T GetVolumeComponentDefaultState<T>() where T : VolumeComponent
        {
            if (!VolumeManager.instance.isInitialized)
                return null;

            return VolumeManager.instance.GetVolumeComponentDefaultState(typeof(T)) as T;
        }

        /// <summary>
        /// Finds which default volume profile (global or quality) is setting a parameter value.
        /// Checks in precedence order: Quality profile overrides Global profile.
        /// </summary>
        /// <typeparam name="T">The type of volume component</typeparam>
        /// <param name="parameterPredicate">Predicate which specifies the target</param>
        /// <param name="sourceProfile">The volume profile that is setting the value, or null if not found</param>
        /// <param name="sourceDescription">Human-readable description of the source (e.g., "Quality Profile in HDRenderPipelineAsset")</param>
        /// <returns>True if a profile with the target parameter was found</returns>
        internal static bool TryGetVolumeParameterSource<T>(
            System.Func<T, bool> parameterPredicate,
            out VolumeProfile sourceProfile,
            out string sourceDescription) where T : VolumeComponent
        {
            sourceProfile = null;
            sourceDescription = null;

            if (!VolumeManager.instance.isInitialized)
                return false;

            // Check quality profile first (higher precedence)
            if (VolumeManager.instance.qualityDefaultProfile != null &&
                VolumeManager.instance.qualityDefaultProfile.TryGet<T>(out var qualityComponent) &&
                qualityComponent.active && parameterPredicate(qualityComponent))
            {
                sourceProfile = VolumeManager.instance.qualityDefaultProfile;
                var assetName = HDRenderPipeline.currentAsset != null ? HDRenderPipeline.currentAsset.name : "HDRP Asset";
                sourceDescription = $"Quality Profile ({assetName})";
                return true;
            }

            // Check global default profile (lower precedence)
            if (VolumeManager.instance.globalDefaultProfile != null &&
                VolumeManager.instance.globalDefaultProfile.TryGet<T>(out var globalComponent) &&
                globalComponent.active && parameterPredicate(globalComponent))
            {
                sourceProfile = VolumeManager.instance.globalDefaultProfile;
                sourceDescription = "Global Profile (Graphics Settings)";
                return true;
            }

            return false;
        }

        /// <summary>
        /// Shows a platform-specific performance warning help box for a given feature.
        /// </summary>
        /// <param name="featureName">The name of the feature (e.g., "Ray Tracing", "Film Grain")</param>
        /// <param name="recommendation">Optional recommendation text. If null, uses default "is not recommended for this platform"</param>
        internal static void ShowFeatureOptimisationWarning(string featureName, string recommendation = null)
        {
            string message = $"{featureName} is enabled for the active platform.\n";

            if (!string.IsNullOrEmpty(recommendation))
            {
                message += recommendation;
            }
            else
            {
                message += HDRenderPipelineUI.Styles.featureNotRecommendedWarning;
            }

            EditorGUILayout.HelpBox(message, MessageType.Warning, wide: true);
        }

        /// <summary>
        /// Shows a platform-specific performance warning help box for a given feature.
        /// </summary>
        /// <param name="featureName">The name of the feature (e.g., "Ray Tracing", "Film Grain")</param>
        /// <param name="recommendation">Optional recommendation text. If null, uses default "is not recommended for this platform"</param>
        internal static void ShowFeatureOptimisationWarning(string featureName, string sourceAssetName, Action onButtonClicked, string recommendation = null)
        {
            string message = $"{featureName} is enabled in {sourceAssetName} for the active platform.\n";

            if (!string.IsNullOrEmpty(recommendation))
            {
                message += recommendation;
            }
            else
            {
                message += HDRenderPipelineUI.Styles.featureNotRecommendedWarning;
            }

            CoreEditorUtils.DrawFixMeBox(
                message,
                MessageType.Warning,
                "Open",
                onButtonClicked);
        }

        internal static void ShowFeatureParameterOptimisationWarning(string settingName, string settingValue, string recommendation = null)
        {
            EditorGUILayout.HelpBox(CreateParameterWarningMessage(settingName, settingValue, null, recommendation), MessageType.Warning, wide: true);
        }

        internal static void ShowFeatureParameterOptimisationWarning(string settingName, string settingValue, string sourceAssetName, Action onButtonClicked, string recommendation = null)
        {
            CoreEditorUtils.DrawFixMeBox(
                CreateParameterWarningMessage(settingName, settingValue, sourceAssetName, recommendation),
                MessageType.Warning,
                "Open",
                onButtonClicked);
        }

        internal static string CreateParameterWarningMessage(string settingName, string settingValue, string sourceAssetName = null, string recommendation = null)
        {
            string message = $"{settingName}: {settingValue} ";
            if (sourceAssetName != null)
            {
                message += $"is set in {sourceAssetName}.";
            }
            else
            {
                message += $"is used for the active platform.";
            }

            if (!string.IsNullOrEmpty(recommendation))
            {
                message += '\n' + recommendation;
            }
            else
            {
                message += $"\nThis may impact performance and is not recommended for this platform.";
            }

            return message;
        }

        internal static bool IsInTestSuiteOrBatchMode()
        {
            string commandLineOptions = System.Environment.CommandLine;
            bool inTestSuite = commandLineOptions.Contains("-testResults");
            return inTestSuite || Application.isBatchMode;
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
