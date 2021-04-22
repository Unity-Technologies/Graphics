using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    using CED = CoreEditorDrawer<SerializedHDCamera>;

    static partial class HDCameraUI
    {
        partial class PhysicalCamera
        {
            public static readonly CED.IDrawer Drawer = CED.Conditional(
                (serialized, owner) => serialized.projectionMatrixMode.intValue == (int)ProjectionMatrixMode.PhysicalPropertiesBased,
                CED.Group(
                    Styles.cameraBody,
                    GroupOption.Indent,
                    CED.Group(
                        GroupOption.Indent,
                        Drawer_PhysicalCamera_CameraBody_Sensor,
                        Drawer_PhysicalCamera_CameraBody_ISO,
                        Drawer_PhysicalCamera_CameraBody_ShutterSpeed,
                        Drawer_PhysicalCamera_CameraBody_GateFit
                    )
                    ),
                CED.Group(
                    Styles.lens,
                    GroupOption.Indent,
                    CED.Group(
                        GroupOption.Indent,
                        Drawer_PhysicalCamera_Lens_FocalLength,
                        Drawer_PhysicalCamera_Lens_Shift,
                        Drawer_PhysicalCamera_Lens_Aperture
                    )
                    ),
                CED.Group(
                    Styles.apertureShape,
                    GroupOption.Indent,
                    CED.Group(
                        GroupOption.Indent,
                        Drawer_PhysicalCamera_ApertureShape
                    )
                )
            );

            // Saves the value of the sensor size when the user switches from "custom" size to a preset per camera.
            // We use a ConditionalWeakTable instead of a Dictionary to avoid keeping alive (with strong references) deleted cameras
            static ConditionalWeakTable<Camera, object> s_PerCameraSensorSizeHistory = new ConditionalWeakTable<Camera, object>();

            static void Drawer_PhysicalCamera_CameraBody_Sensor(SerializedHDCamera p, Editor owner)
            {
                var cam = p.baseCameraSettings;
                EditorGUI.BeginChangeCheck();

                int oldFilmGateIndex = Array.IndexOf(Styles.apertureFormatValues, new Vector2((float)Math.Round(cam.sensorSize.vector2Value.x, 3), (float)Math.Round(cam.sensorSize.vector2Value.y, 3)));

                // If it is not one of the preset sizes, set it to custom
                oldFilmGateIndex = (oldFilmGateIndex == -1) ? Styles.customPresetIndex : oldFilmGateIndex;

                // Get the new user selection
                int newFilmGateIndex = EditorGUILayout.Popup(Styles.sensorType, oldFilmGateIndex, Styles.apertureFormatNames);

                if (EditorGUI.EndChangeCheck())
                {
                    // Retrieve the previous custom size value, if one exists for this camera
                    object previousCustomValue;
                    s_PerCameraSensorSizeHistory.TryGetValue((Camera)p.serializedObject.targetObject, out previousCustomValue);

                    // When switching from custom to a preset, update the last custom value (to display again, in case the user switches back to custom)
                    if (oldFilmGateIndex == Styles.customPresetIndex)
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

                    if (newFilmGateIndex < Styles.customPresetIndex)
                    {
                        cam.sensorSize.vector2Value = Styles.apertureFormatValues[newFilmGateIndex];
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

                EditorGUILayout.PropertyField(cam.sensorSize, Styles.sensorSize);
            }

            static void Drawer_PhysicalCamera_CameraBody_GateFit(SerializedHDCamera p, Editor owner)
            {
                var cam = p.baseCameraSettings;

                using (var horizontal = new EditorGUILayout.HorizontalScope())
                using (var propertyScope = new EditorGUI.PropertyScope(horizontal.rect, Styles.gateFit, cam.gateFit))
                using (var checkScope = new EditorGUI.ChangeCheckScope())
                {
                    int gateValue = (int)(Camera.GateFitMode)EditorGUILayout.EnumPopup(propertyScope.content, (Camera.GateFitMode)cam.gateFit.intValue);
                    if (checkScope.changed)
                        cam.gateFit.intValue = gateValue;
                }
            }

            static void Drawer_PhysicalCamera_Lens_FocalLength(SerializedHDCamera p, Editor owner)
            {
                var cam = p.baseCameraSettings;

                using (var horizontal = new EditorGUILayout.HorizontalScope())
                using (new EditorGUI.PropertyScope(horizontal.rect, Styles.focalLength, cam.focalLength))
                using (var checkScope = new EditorGUI.ChangeCheckScope())
                {
                    bool isPhysical = p.projectionMatrixMode.intValue == (int)ProjectionMatrixMode.PhysicalPropertiesBased;
                    // We need to update the focal length if the camera is physical and the FoV has changed.
                    bool focalLengthIsDirty = (s_FovChanged && isPhysical);

                    float sensorLength = cam.fovAxisMode.intValue == 0 ? cam.sensorSize.vector2Value.y : cam.sensorSize.vector2Value.x;
                    float focalLengthVal = focalLengthIsDirty ? Camera.FieldOfViewToFocalLength(s_FovLastValue, sensorLength) : cam.focalLength.floatValue;
                    focalLengthVal = EditorGUILayout.FloatField(Styles.focalLength, focalLengthVal);
                    if (checkScope.changed || focalLengthIsDirty)
                        cam.focalLength.floatValue = focalLengthVal;
                }
            }

            static void Drawer_PhysicalCamera_Lens_Shift(SerializedHDCamera p, Editor owner)
            {
                EditorGUILayout.PropertyField(p.baseCameraSettings.lensShift, Styles.shift);
            }

            static void Drawer_PhysicalCamera_CameraBody_ISO(SerializedHDCamera p, Editor owner)
            {
                EditorGUILayout.PropertyField(p.iso, Styles.ISO);
            }

            static EditorPrefBoolFlags<ShutterSpeedUnit> m_ShutterSpeedState = new EditorPrefBoolFlags<ShutterSpeedUnit>($"HDRP:{typeof(HDCameraUI).Name}:ShutterSpeedState");

            enum ShutterSpeedUnit
            {
                Second,
                OneOverSecond
            }

            static readonly string[] k_ShutterSpeedUnitNames =
            {
                "Second",
                "1 \u2215 Second" // Don't use a slash here else Unity will auto-create a submenu...
            };

            static void Drawer_PhysicalCamera_CameraBody_ShutterSpeed(SerializedHDCamera p, Editor owner)
            {
                // Custom layout for shutter speed
                const int k_UnitMenuWidth = 90;
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

                EditorGUI.BeginProperty(fieldRect, Styles.shutterSpeed, p.shutterSpeed);
                {
                    // if we we use (1 / second) units, then change the value for the display and then revert it back
                    if (m_ShutterSpeedState.value == ShutterSpeedUnit.OneOverSecond && p.shutterSpeed.floatValue > 0)
                        p.shutterSpeed.floatValue = 1.0f / p.shutterSpeed.floatValue;
                    EditorGUI.PropertyField(fieldRect, p.shutterSpeed, Styles.shutterSpeed);
                    if (m_ShutterSpeedState.value == ShutterSpeedUnit.OneOverSecond && p.shutterSpeed.floatValue > 0)
                        p.shutterSpeed.floatValue = 1.0f / p.shutterSpeed.floatValue;
                }
                EditorGUI.EndProperty();
            }

            static void Drawer_PhysicalCamera_Lens_Aperture(SerializedHDCamera p, Editor owner)
            {
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
                    EditorGUI.LabelField(labelRect, Styles.aperture);

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
                    if (float.TryParse(newAperture, out float parsedValue))
                        p.aperture.floatValue = Mathf.Clamp(parsedValue, HDPhysicalCamera.kMinAperture, HDPhysicalCamera.kMaxAperture);
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
            }

            static void Drawer_PhysicalCamera_ApertureShape(SerializedHDCamera p, Editor owner)
            {
                var cam = p.baseCameraSettings;

                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(p.bladeCount, Styles.bladeCount);

                    using (var horizontal = new EditorGUILayout.HorizontalScope())
                    using (var propertyScope = new EditorGUI.PropertyScope(horizontal.rect, Styles.curvature, p.curvature))
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

                    EditorGUILayout.PropertyField(p.barrelClipping, Styles.barrelClipping);
                    EditorGUILayout.PropertyField(p.anamorphism, Styles.anamorphism);
                }
            }
        }
    }
}
