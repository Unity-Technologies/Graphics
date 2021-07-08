using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace UnityEditor.Rendering
{
    /// <summary> Camera UI Shared Properties among SRP</summary>
    public static partial class CameraUI
    {
        /// <summary>
        /// Physical camera related drawers
        /// </summary>
        public static partial class PhysicalCamera
        {
            // Saves the value of the sensor size when the user switches from "custom" size to a preset per camera.
            // We use a ConditionalWeakTable instead of a Dictionary to avoid keeping alive (with strong references) deleted cameras
            static ConditionalWeakTable<Camera, object> s_PerCameraSensorSizeHistory = new ConditionalWeakTable<Camera, object>();

            /// <summary>Draws Body Sensor related fields on the inspector</summary>
            /// <param name="p"><see cref="ISerializedCamera"/> The serialized camera</param>
            /// <param name="owner"><see cref="Editor"/> The editor owner calling this drawer</param>
            public static void Drawer_PhysicalCamera_CameraBody_Sensor(ISerializedCamera p, Editor owner)
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

            /// <summary>Draws Gate fit related fields on the inspector</summary>
            /// <param name="p"><see cref="ISerializedCamera"/> The serialized camera</param>
            /// <param name="owner"><see cref="Editor"/> The editor owner calling this drawer</param>
            public static void Drawer_PhysicalCamera_CameraBody_GateFit(ISerializedCamera p, Editor owner)
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

            /// <summary>Draws Focal Length related fields on the inspector</summary>
            /// <param name="p"><see cref="ISerializedCamera"/> The serialized camera</param>
            /// <param name="owner"><see cref="Editor"/> The editor owner calling this drawer</param>
            public static void Drawer_PhysicalCamera_Lens_FocalLength(ISerializedCamera p, Editor owner)
            {
                var cam = p.baseCameraSettings;

                using (var horizontal = new EditorGUILayout.HorizontalScope())
                using (new EditorGUI.PropertyScope(horizontal.rect, Styles.focalLength, cam.focalLength))
                using (var checkScope = new EditorGUI.ChangeCheckScope())
                {
                    bool isPhysical = p.projectionMatrixMode.intValue == (int)CameraUI.ProjectionMatrixMode.PhysicalPropertiesBased;
                    // We need to update the focal length if the camera is physical and the FoV has changed.
                    bool focalLengthIsDirty = (s_FovChanged && isPhysical);

                    float sensorLength = cam.fovAxisMode.intValue == 0 ? cam.sensorSize.vector2Value.y : cam.sensorSize.vector2Value.x;
                    float focalLengthVal = focalLengthIsDirty ? Camera.FieldOfViewToFocalLength(s_FovLastValue, sensorLength) : cam.focalLength.floatValue;
                    focalLengthVal = EditorGUILayout.FloatField(Styles.focalLength, focalLengthVal);
                    if (checkScope.changed || focalLengthIsDirty)
                        cam.focalLength.floatValue = focalLengthVal;
                }
            }

            /// <summary>Draws Lens Shift related fields on the inspector</summary>
            /// <param name="p"><see cref="ISerializedCamera"/> The serialized camera</param>
            /// <param name="owner"><see cref="Editor"/> The editor owner calling this drawer</param>
            public static void Drawer_PhysicalCamera_Lens_Shift(ISerializedCamera p, Editor owner)
            {
                EditorGUILayout.PropertyField(p.baseCameraSettings.lensShift, Styles.shift);
            }
        }
    }
}
