using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    using CED = CoreEditorDrawer<SerializedHDCamera>;

    static partial class HDCameraUI
    {
        enum Expandable
        {
            General = 1 << 0,
            Physical = 1 << 1,
            Output = 1 << 2,
            Orthographic = 1 << 3,
            RenderLoop = 1 << 4,
        }

        enum ProjectionType
        {
            Perspective,
            Orthographic
        }

        enum ProjectionMatrixMode
        {
            Explicit,
            Implicit,
            PhysicalPropertiesBased,
        }

        enum ShutterSpeedUnit
        {
            Second,
            OneOverSecond
        }

        static EditorPrefBoolFlags<ShutterSpeedUnit> m_ShutterSpeedState;

        static readonly string[] k_ShutterSpeedUnitNames =
        {
            "Second",
            "1 \u2215 Second" // Don't use a slash here else Unity will auto-create a submenu...
        };

        static readonly string[] k_ApertureFormatNames =
        {
            "8mm",
            "Super 8mm",
            "16mm",
            "Super 16mm",
            "35mm 2-perf",
            "35mm Academy",
            "Super-35",
            "65mm ALEXA",
            "70mm",
            "70mm IMAX",
            "Custom"
        };

        static readonly int k_CustomPresetIndex = k_ApertureFormatNames.Length - 1;

        static readonly Vector2[] k_ApertureFormatValues =
        {
            new Vector2(4.8f, 3.5f),
            new Vector2(5.79f, 4.01f),
            new Vector2(10.26f, 7.49f),
            new Vector2(12.52f, 7.41f),
            new Vector2(21.95f, 9.35f),
            new Vector2(21f, 15.2f),
            new Vector2(24.89f, 18.66f),
            new Vector2(54.12f, 25.59f),
            new Vector2(70f, 51f),
            new Vector2(70.41f, 52.63f)
        };

        // Saves the value of the sensor size when the user switches from "custom" size to a preset per camera.
        // We use a ConditionalWeakTable instead of a Dictionary to avoid keeping alive (with strong references) deleted cameras
        static ConditionalWeakTable<Camera, object> s_PerCameraSensorSizeHistory = new ConditionalWeakTable<Camera, object>();

        static bool s_FovChanged;
        static float s_FovLastValue;

        static readonly ExpandedState<Expandable, Camera> k_ExpandedState = new ExpandedState<Expandable, Camera>(Expandable.General, "HDRP");

        static HDCameraUI()
        {
            Inspector = new[]
            {
                CED.space,
                SectionGeneralSettings,
                SectionFrameSettings,
                SectionPhysicalSettings,
                SectionOutputSettings,
            };

            string key = $"HDRP:{typeof(HDCameraUI).Name}:ShutterSpeedState";
            m_ShutterSpeedState = new EditorPrefBoolFlags<ShutterSpeedUnit>(key);
        }

        public static readonly CED.IDrawer[] Inspector = null;

        public static readonly CED.IDrawer SectionGeneralSettings = CED.FoldoutGroup(
            generalSettingsHeaderContent,
            Expandable.General,
            k_ExpandedState,
            FoldoutOption.Indent | FoldoutOption.NoSpaceAtEnd, //no space as FrameSettings is drawn just under
            CED.Group(
                Drawer_FieldClear,
                Drawer_FieldCullingMask,
                Drawer_FieldVolumeLayerMask,
                Drawer_FieldVolumeAnchorOverride,
                (p, owner) => EditorGUILayout.PropertyField(p.probeLayerMask, probeLayerMaskContent),
                Drawer_FieldOcclusionCulling,
                Drawer_FieldExposureTarget
                ),
            CED.space,
            CED.Group(
                Drawer_Projection,
                Drawer_FieldClippingPlanes
                ),
            CED.space,
            CED.Group(
                Drawer_Antialiasing,
                Drawer_Dithering,
                Drawer_StopNaNs
                ),
            CED.space,
            CED.Group(
                Drawer_AllowDynamicResolution
                ),
            CED.space,
            CED.Group(
                Drawer_CameraWarnings,
                Drawer_FieldRenderingPath
                )
            );

        public static readonly CED.IDrawer SectionPhysicalSettings = CED.FoldoutGroup(
            physicalSettingsHeaderContent,
            Expandable.Physical,
            k_ExpandedState,
            CED.Group(
                Drawer_PhysicalCamera
                )
            );

        public static readonly CED.IDrawer SectionOutputSettings = CED.FoldoutGroup(
            outputSettingsHeaderContent,
            Expandable.Output,
            k_ExpandedState,
            CED.Group(
#if ENABLE_VR && ENABLE_XR_MANAGEMENT
                Drawer_SectionXRRendering,
#endif
#if ENABLE_MULTIPLE_DISPLAYS
                Drawer_SectionMultiDisplay,
#endif
                Drawer_FieldRenderTarget,
                Drawer_FieldDepth,
                Drawer_FieldNormalizedViewPort
                )
            );

        public static readonly CED.IDrawer SectionFrameSettings = CED.Conditional(
            (serialized, owner) => k_ExpandedState[Expandable.General],
            CED.Group((serialized, owner) =>
            {
                if (!serialized.passThrough.boolValue && serialized.customRenderingSettings.boolValue)
                    FrameSettingsUI.Inspector().Draw(serialized.frameSettings, owner);
                else
                    EditorGUILayout.Space();
            })
        );

        static void Drawer_FieldVolumeLayerMask(SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.volumeLayerMask, volumeLayerMaskContent);
        }
        static void Drawer_FieldVolumeAnchorOverride(SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.volumeAnchorOverride, volumeAnchorOverrideContent);
        }

        static void Drawer_FieldCullingMask(SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.baseCameraSettings.cullingMask, cullingMaskContent);
        }

        static void Drawer_Projection(SerializedHDCamera p, Editor owner)
        {
            // Most of this is replicated from CameraEditor.DrawProjection as we don't want to draw
            // it the same way it's done in non-SRP cameras. Unfortunately, because a lot of the
            // code is internal, we have to copy/paste some stuff from the editor code :(

            var cam = p.baseCameraSettings;

            Rect perspectiveRect = EditorGUILayout.GetControlRect();

            ProjectionType projectionType;
            EditorGUI.BeginProperty(perspectiveRect, projectionContent, cam.orthographic);
            {
                projectionType = cam.orthographic.boolValue ? ProjectionType.Orthographic : ProjectionType.Perspective;

                EditorGUI.BeginChangeCheck();
                projectionType = (ProjectionType)EditorGUI.EnumPopup(perspectiveRect, projectionContent, projectionType);
                if (EditorGUI.EndChangeCheck())
                    cam.orthographic.boolValue = (projectionType == ProjectionType.Orthographic);
            }
            EditorGUI.EndProperty();
            
            if (cam.orthographic.hasMultipleDifferentValues)
                return;

            if (projectionType == ProjectionType.Orthographic)
            {
                EditorGUILayout.PropertyField(cam.orthographicSize, sizeContent);
            }
            else
            {
                float fovCurrentValue;
                bool multipleDifferentFovValues = false;
                bool isPhysicalCamera = p.projectionMatrixMode.intValue == (int)ProjectionMatrixMode.PhysicalPropertiesBased;

                var rect = EditorGUILayout.GetControlRect();

                var guiContent = EditorGUI.BeginProperty(rect, FOVAxisModeContent, cam.fovAxisMode);
                EditorGUI.showMixedValue = cam.fovAxisMode.hasMultipleDifferentValues;

                EditorGUI.BeginChangeCheck();
                var fovAxisNewVal = (int)(Camera.FieldOfViewAxis)EditorGUI.EnumPopup(rect, guiContent, (Camera.FieldOfViewAxis)cam.fovAxisMode.intValue);
                if (EditorGUI.EndChangeCheck())
                    cam.fovAxisMode.intValue = fovAxisNewVal;
                EditorGUI.EndProperty();

                bool fovAxisVertical = cam.fovAxisMode.intValue == 0;

                if (!fovAxisVertical && !cam.fovAxisMode.hasMultipleDifferentValues)
                {
                    var targets = p.serializedObject.targetObjects;
                    var camera0 = targets[0] as Camera;
                    float aspectRatio = isPhysicalCamera ? cam.sensorSize.vector2Value.x / cam.sensorSize.vector2Value.y : camera0.aspect;
                    // camera.aspect is not serialized so we have to check all targets.
                    fovCurrentValue = Camera.VerticalToHorizontalFieldOfView(camera0.fieldOfView, aspectRatio);
                    if (targets.Cast<Camera>().Any(camera => camera.fieldOfView != fovCurrentValue))
                        multipleDifferentFovValues = true;
                }
                else
                {
                    fovCurrentValue = cam.verticalFOV.floatValue;
                    multipleDifferentFovValues = cam.fovAxisMode.hasMultipleDifferentValues;
                }

                EditorGUI.showMixedValue = multipleDifferentFovValues;
                var content = EditorGUI.BeginProperty(EditorGUILayout.BeginHorizontal(), fieldOfViewContent, cam.verticalFOV);
                EditorGUI.BeginDisabledGroup(p.projectionMatrixMode.hasMultipleDifferentValues || isPhysicalCamera && (cam.sensorSize.hasMultipleDifferentValues || cam.fovAxisMode.hasMultipleDifferentValues));
                EditorGUI.BeginChangeCheck();
                s_FovLastValue = EditorGUILayout.Slider(content, fovCurrentValue, 0.00001f, 179f);
                s_FovChanged = EditorGUI.EndChangeCheck();
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();
                EditorGUI.EndProperty();
                EditorGUI.showMixedValue = false;

                content = EditorGUI.BeginProperty(EditorGUILayout.BeginHorizontal(), physicalCameraContent, p.projectionMatrixMode);
                EditorGUI.showMixedValue = p.projectionMatrixMode.hasMultipleDifferentValues;

                EditorGUI.BeginChangeCheck();
                isPhysicalCamera = EditorGUILayout.Toggle(content, isPhysicalCamera);
                if (EditorGUI.EndChangeCheck())
                    p.projectionMatrixMode.intValue = isPhysicalCamera ? (int)ProjectionMatrixMode.PhysicalPropertiesBased : (int)ProjectionMatrixMode.Implicit;
                EditorGUILayout.EndHorizontal();
                EditorGUI.EndProperty();

                EditorGUI.showMixedValue = false;
                if (s_FovChanged && (!isPhysicalCamera || p.projectionMatrixMode.hasMultipleDifferentValues))
                {
                    cam.verticalFOV.floatValue = fovAxisVertical
                        ? s_FovLastValue
                        : Camera.HorizontalToVerticalFieldOfView(s_FovLastValue, (p.serializedObject.targetObjects[0] as Camera).aspect);
                }
                else if (s_FovChanged && isPhysicalCamera && !p.projectionMatrixMode.hasMultipleDifferentValues)
                {
                    // If we have a physical camera, we should also update the focal length here, because the
                    // Drawer_PhysicalCamera will not be executed if the physical camera fold-out is closed
                    cam.verticalFOV.floatValue = fovAxisVertical
                        ? s_FovLastValue
                        : Camera.HorizontalToVerticalFieldOfView(s_FovLastValue, (p.serializedObject.targetObjects[0] as Camera).aspect);

                    float sensorLength = cam.fovAxisMode.intValue == 0 ? cam.sensorSize.vector2Value.y : cam.sensorSize.vector2Value.x;
                    float focalLengthVal = Camera.FieldOfViewToFocalLength(s_FovLastValue, sensorLength);
                    cam.focalLength.floatValue = EditorGUILayout.FloatField(focalLengthContent, focalLengthVal);
                }

                EditorGUILayout.Space();
            }
        }

        static void Drawer_FieldClippingPlanes(SerializedHDCamera p, Editor owner)
        {
            CoreEditorUtils.DrawMultipleFields(
                clippingPlaneMultiFieldTitle,
                new[] { p.baseCameraSettings.nearClippingPlane, p.baseCameraSettings.farClippingPlane },
                new[] { nearPlaneContent, farPlaneContent });
        }

        static void Drawer_PhysicalCamera(SerializedHDCamera p, Editor owner)
        {
            var cam = p.baseCameraSettings;

            EditorGUILayout.LabelField("Camera Body", EditorStyles.boldLabel);

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUI.BeginChangeCheck();

                int oldFilmGateIndex = Array.IndexOf(k_ApertureFormatValues, new Vector2((float)Math.Round(cam.sensorSize.vector2Value.x, 3), (float)Math.Round(cam.sensorSize.vector2Value.y, 3)));

                // If it is not one of the preset sizes, set it to custom
                oldFilmGateIndex = (oldFilmGateIndex == -1) ? k_CustomPresetIndex: oldFilmGateIndex;

                // Get the new user selection
                int newFilmGateIndex = EditorGUILayout.Popup(cameraTypeContent, oldFilmGateIndex, k_ApertureFormatNames);

                if (EditorGUI.EndChangeCheck())
                {
                    // Retrieve the previous custom size value, if one exists for this camera
                    object previousCustomValue;
                    s_PerCameraSensorSizeHistory.TryGetValue((Camera)p.serializedObject.targetObject, out previousCustomValue);

                    // When switching from custom to a preset, update the last custom value (to display again, in case the user switches back to custom)
                    if (oldFilmGateIndex == k_CustomPresetIndex)
                    {
                        if (previousCustomValue == null)
                        {
                            s_PerCameraSensorSizeHistory.Add((Camera)p.serializedObject.targetObject, cam.sensorSize.vector2Value);
                        }
                        else
                        {
                            previousCustomValue = cam.sensorSize.vector2Value;
                        }
                    }

                    if (newFilmGateIndex < k_CustomPresetIndex)
                    {
                        cam.sensorSize.vector2Value = k_ApertureFormatValues[newFilmGateIndex];
                    }
                    else
                    {
                        // The user switched back to custom, so display by deafulr the previous custom value
                        if (previousCustomValue != null)
                        {
                            cam.sensorSize.vector2Value = (Vector2)previousCustomValue;
                        }
                        else
                        {
                            cam.sensorSize.vector2Value = new Vector2(36.0f, 24.0f); // this is the value new cameras are created with
                        }
                    }
                }

                EditorGUILayout.PropertyField(cam.sensorSize, sensorSizeContent);
                EditorGUILayout.PropertyField(p.iso, isoContent);

                // Custom layout for shutter speed
                const int k_UnitMenuWidth = 80;
                const int k_OffsetPerIndent = 15;
                const int k_LabelFieldSeparator = 2;
                const int k_Offset = 1;
                int oldIndentLevel = EditorGUI.indentLevel;

                // Don't take into account the indentLevel when rendering the units field
                EditorGUI.indentLevel = 0;

                var lineRect = EditorGUILayout.GetControlRect();
                var fieldRect = new Rect(k_OffsetPerIndent + k_LabelFieldSeparator + k_Offset, lineRect.y, lineRect.width - k_UnitMenuWidth, lineRect.height);
                var unitMenu = new Rect(fieldRect.xMax + k_LabelFieldSeparator, lineRect.y, k_UnitMenuWidth - k_LabelFieldSeparator, lineRect.height);

                // We cannot had the shutterSpeedState as this is not a serialized property but a global edition mode.
                // This imply that it will never go bold nor can be reverted in prefab overrides

                m_ShutterSpeedState.value = (ShutterSpeedUnit)EditorGUI.Popup(unitMenu, (int)m_ShutterSpeedState.value, k_ShutterSpeedUnitNames);
                // Reset the indent level
                EditorGUI.indentLevel = oldIndentLevel;

                EditorGUI.BeginProperty(fieldRect, shutterSpeedContent, p.shutterSpeed);
                {
                    // if we we use (1 / second) units, then change the value for the display and then revert it back
                    if (m_ShutterSpeedState.value == ShutterSpeedUnit.OneOverSecond && p.shutterSpeed.floatValue > 0)
                        p.shutterSpeed.floatValue = 1.0f / p.shutterSpeed.floatValue;
                    EditorGUI.PropertyField(fieldRect, p.shutterSpeed, shutterSpeedContent);
                    if (m_ShutterSpeedState.value == ShutterSpeedUnit.OneOverSecond && p.shutterSpeed.floatValue > 0)
                        p.shutterSpeed.floatValue = 1.0f / p.shutterSpeed.floatValue;
                }
                EditorGUI.EndProperty();

                using (var horizontal = new EditorGUILayout.HorizontalScope())
                using (var propertyScope = new EditorGUI.PropertyScope(horizontal.rect, gateFitContent, cam.gateFit))
                using (var checkScope = new EditorGUI.ChangeCheckScope())
                {
                    int gateValue = (int)(Camera.GateFitMode)EditorGUILayout.EnumPopup(propertyScope.content, (Camera.GateFitMode)cam.gateFit.intValue);
                    if (checkScope.changed)
                        cam.gateFit.intValue = gateValue;
                }
            }

            EditorGUILayout.LabelField("Lens", EditorStyles.boldLabel);

            using (new EditorGUI.IndentLevelScope())
            {
                using (var horizontal = new EditorGUILayout.HorizontalScope())
                using (new EditorGUI.PropertyScope(horizontal.rect, focalLengthContent, cam.focalLength))
                using (var checkScope = new EditorGUI.ChangeCheckScope())
                {
                    bool isPhysical = p.projectionMatrixMode.intValue == (int)ProjectionMatrixMode.PhysicalPropertiesBased;
                    // We need to update the focal length if the camera is physical and the FoV has changed.
                    bool focalLengthIsDirty = (s_FovChanged && isPhysical);

                    float sensorLength = cam.fovAxisMode.intValue == 0 ? cam.sensorSize.vector2Value.y : cam.sensorSize.vector2Value.x;
                    float focalLengthVal = focalLengthIsDirty ? Camera.FieldOfViewToFocalLength(s_FovLastValue, sensorLength) : cam.focalLength.floatValue;
                    focalLengthVal = EditorGUILayout.FloatField(focalLengthContent, focalLengthVal);
                    if (checkScope.changed || focalLengthIsDirty)
                        cam.focalLength.floatValue = focalLengthVal;
                }

                // Custom layout for aperture
                var rect = EditorGUILayout.BeginHorizontal();
                {
                    // Magic values/offsets to get the UI look consistent
                    const float textRectSize = 80;
                    const float textRectPaddingRight = 62;
                    const float unitRectPaddingRight = 97;
                    const float sliderPaddingLeft = 2;
                    const float sliderPaddingRight = 77;

                    var labelRect = rect;
                    labelRect.width = EditorGUIUtility.labelWidth;
                    labelRect.height = EditorGUIUtility.singleLineHeight;
                    EditorGUI.LabelField(labelRect, apertureContent);

                    GUI.SetNextControlName("ApertureSlider");
                    var sliderRect = rect;
                    sliderRect.x += labelRect.width + sliderPaddingLeft;
                    sliderRect.width = rect.width - labelRect.width - sliderPaddingRight;
                    float newVal = GUI.HorizontalSlider(sliderRect, p.aperture.floatValue, HDPhysicalCamera.kMinAperture, HDPhysicalCamera.kMaxAperture);

                    // keep only 2 digits of precision, like the otehr editor fields
                    newVal = Mathf.Floor(100 * newVal) / 100.0f;

                    if (p.aperture.floatValue != newVal)
                    {
                        p.aperture.floatValue = newVal;
                        // Note: We need to move the focus when the slider changes, otherwise the textField will not update
                        GUI.FocusControl("ApertureSlider");
                    }

                    var unitRect = rect;
                    unitRect.x += rect.width - unitRectPaddingRight;
                    unitRect.width = textRectSize;
                    unitRect.height = EditorGUIUtility.singleLineHeight;
                    EditorGUI.LabelField(unitRect, "f /", EditorStyles.label);

                    var textRect = rect;
                    textRect.x = rect.width - textRectPaddingRight;
                    textRect.width = textRectSize;
                    textRect.height = EditorGUIUtility.singleLineHeight;
                    string newAperture = EditorGUI.TextField(textRect, p.aperture.floatValue.ToString());
                    try
                    {
                        p.aperture.floatValue = Mathf.Clamp(float.Parse(newAperture), HDPhysicalCamera.kMinAperture, HDPhysicalCamera.kMaxAperture);
                    }
                    catch
                    { }
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
                EditorGUILayout.PropertyField(cam.lensShift, lensShiftContent);
            }

            EditorGUILayout.LabelField("Aperture Shape", EditorStyles.boldLabel);

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(p.bladeCount, bladeCountContent);

                using (var horizontal = new EditorGUILayout.HorizontalScope())
                using (var propertyScope = new EditorGUI.PropertyScope(horizontal.rect, curvatureContent, p.curvature))
                {
                    var v = p.curvature.vector2Value;

                    // The layout system breaks alignment when mixing inspector fields with custom layout'd
                    // fields as soon as a scrollbar is needed in the inspector, so we'll do the layout
                    // manually instead
                    const int kFloatFieldWidth = 50;
                    const int kSeparatorWidth = 5;
                    float indentOffset = EditorGUI.indentLevel * 15f;
                    var lineRect = EditorGUILayout.GetControlRect();
                    var labelRect = new Rect(lineRect.x, lineRect.y, EditorGUIUtility.labelWidth - indentOffset, lineRect.height);
                    var floatFieldLeft = new Rect(labelRect.xMax, lineRect.y, kFloatFieldWidth + indentOffset, lineRect.height);
                    var sliderRect = new Rect(floatFieldLeft.xMax + kSeparatorWidth - indentOffset, lineRect.y, lineRect.width - labelRect.width - kFloatFieldWidth * 2 - kSeparatorWidth * 2, lineRect.height);
                    var floatFieldRight = new Rect(sliderRect.xMax + kSeparatorWidth - indentOffset, lineRect.y, kFloatFieldWidth + indentOffset, lineRect.height);

                    EditorGUI.PrefixLabel(labelRect, propertyScope.content);
                    v.x = EditorGUI.FloatField(floatFieldLeft, v.x);
                    EditorGUI.MinMaxSlider(sliderRect, ref v.x, ref v.y, HDPhysicalCamera.kMinAperture, HDPhysicalCamera.kMaxAperture);
                    v.y = EditorGUI.FloatField(floatFieldRight, v.y);

                    p.curvature.vector2Value = v;
                }

                EditorGUILayout.PropertyField(p.barrelClipping, barrelClippingContent);
                EditorGUILayout.PropertyField(p.anamorphism, anamorphismContent);
            }
        }

        static void Drawer_FieldNormalizedViewPort(SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.baseCameraSettings.normalizedViewPortRect, viewportContent);
        }

        static void Drawer_FieldDepth(SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.baseCameraSettings.depth, depthContent);
        }

        static void Drawer_FieldClear(SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.clearColorMode, clearModeContent);
            // if(p.clearColorMode.GetEnumValue<HDAdditionalCameraData.ClearColorMode>() == HDAdditionalCameraData.ClearColorMode.BackgroundColor) or no sky in scene
            EditorGUILayout.PropertyField(p.backgroundColorHDR, backgroundColorContent);

            if(p.clearDepth.boolValue == false)
                p.clearDepth.boolValue = true;
        }

        static void Drawer_Antialiasing(SerializedHDCamera p, Editor owner)
        {
            Rect antiAliasingRect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(antiAliasingRect, antialiasingContent, p.antialiasing);
            {
                EditorGUI.BeginChangeCheck();
                int selectedValue = EditorGUI.Popup(antiAliasingRect, antialiasingContent, p.antialiasing.intValue, antialiasingModeNames);
                if (EditorGUI.EndChangeCheck())
                    p.antialiasing.intValue = selectedValue;
            }
            EditorGUI.EndProperty();

            if (p.antialiasing.intValue == (int)HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing)
            {
                EditorGUILayout.PropertyField(p.SMAAQuality, SMAAQualityPresetContent);
            }
            else if(p.antialiasing.intValue == (int)HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing)
            {
                EditorGUILayout.PropertyField(p.taaQualityLevel, TAAQualityLevelContent);

                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(p.taaSharpenStrength, TAASharpenContent);

                if (p.taaQualityLevel.intValue > (int)HDAdditionalCameraData.TAAQualityLevel.Low)
                {
                    EditorGUILayout.PropertyField(p.taaHistorySharpening, TAAHistorySharpening);
                    EditorGUILayout.PropertyField(p.taaAntiFlicker, TAAAntiFlicker);
                }

                if(p.taaQualityLevel.intValue == (int)HDAdditionalCameraData.TAAQualityLevel.High)
                {
                    EditorGUILayout.PropertyField(p.taaMotionVectorRejection, TAAMotionVectorRejection);
                    EditorGUILayout.PropertyField(p.taaAntiRinging, TAAAntiRingingContent);
                }

                EditorGUI.indentLevel--;
            }
        }

        static void Drawer_Dithering(SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.dithering, ditheringContent);
        }

        static void Drawer_StopNaNs(SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.stopNaNs, stopNaNsContent);
        }

        static void Drawer_AllowDynamicResolution(SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.allowDynamicResolution, allowDynResContent);
            p.baseCameraSettings.allowDynamicResolution.boolValue = p.allowDynamicResolution.boolValue;
        }

        static void Drawer_FieldRenderingPath(SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.passThrough, fullScreenPassthroughContent);
            using (new EditorGUI.DisabledScope(p.passThrough.boolValue))
                EditorGUILayout.PropertyField(p.customRenderingSettings, renderingPathContent);
        }

        static void Drawer_FieldRenderTarget(SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.baseCameraSettings.targetTexture);

            // show warning if we have deferred but manual MSAA set
            // only do this if the m_TargetTexture has the same values across all target cameras
            if (!p.baseCameraSettings.targetTexture.hasMultipleDifferentValues)
            {
                var targetTexture = p.baseCameraSettings.targetTexture.objectReferenceValue as RenderTexture;
                if (targetTexture
                    && targetTexture.antiAliasing > 1
                    && p.frameSettings.litShaderMode == LitShaderMode.Deferred)
                {
                    EditorGUILayout.HelpBox(msaaWarningMessage, MessageType.Warning, true);
                }
            }
        }

        static void Drawer_FieldExposureTarget(SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.exposureTarget, exposureTargetContent);
        }

        static void Drawer_FieldOcclusionCulling(SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.baseCameraSettings.occlusionCulling, occlusionCullingContent);
        }

        static void Drawer_CameraWarnings(SerializedHDCamera p, Editor owner)
        {
            foreach (Camera camera in p.serializedObject.targetObjects)
            {
                var warnings = GetCameraBufferWarnings(camera);
                if (warnings.Length > 0)
                    EditorGUILayout.HelpBox(string.Join("\n\n", warnings), MessageType.Warning, true);
            }
        }

        static void Drawer_SectionXRRendering(SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.xrRendering, xrRenderingContent);
        }

#if ENABLE_MULTIPLE_DISPLAYS
        static void Drawer_SectionMultiDisplay(SerializedHDCamera p, Editor owner)
        {
            if (ModuleManager_ShouldShowMultiDisplayOption())
            {
                var prevDisplay = p.baseCameraSettings.targetDisplay.intValue;
                EditorGUILayout.IntPopup(p.baseCameraSettings.targetDisplay, DisplayUtility_GetDisplayNames(), DisplayUtility_GetDisplayIndices(), targetDisplayContent);
                if (prevDisplay != p.baseCameraSettings.targetDisplay.intValue)
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }
        }

#endif

        static MethodInfo k_DisplayUtility_GetDisplayIndices = Type.GetType("UnityEditor.DisplayUtility,UnityEditor")
            .GetMethod("GetDisplayIndices");
        static int[] DisplayUtility_GetDisplayIndices()
        {
            return (int[])k_DisplayUtility_GetDisplayIndices.Invoke(null, null);
        }

        static MethodInfo k_DisplayUtility_GetDisplayNames = Type.GetType("UnityEditor.DisplayUtility,UnityEditor")
            .GetMethod("GetDisplayNames");
        static GUIContent[] DisplayUtility_GetDisplayNames()
        {
            return (GUIContent[])k_DisplayUtility_GetDisplayNames.Invoke(null, null);
        }

        static MethodInfo k_ModuleManager_ShouldShowMultiDisplayOption = Type.GetType("UnityEditor.Modules.ModuleManager,UnityEditor")
            .GetMethod("ShouldShowMultiDisplayOption", BindingFlags.Static | BindingFlags.NonPublic);
        static bool ModuleManager_ShouldShowMultiDisplayOption()
        {
            return (bool)k_ModuleManager_ShouldShowMultiDisplayOption.Invoke(null, null);
        }

        static readonly MethodInfo k_Camera_GetCameraBufferWarnings = typeof(Camera).GetMethod("GetCameraBufferWarnings", BindingFlags.Instance | BindingFlags.NonPublic);
        static string[] GetCameraBufferWarnings(Camera camera)
        {
            return (string[])k_Camera_GetCameraBufferWarnings.Invoke(camera, null);
        }
    }
}
