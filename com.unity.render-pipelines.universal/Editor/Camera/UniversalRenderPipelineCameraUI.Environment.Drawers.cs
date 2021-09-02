namespace UnityEditor.Rendering.Universal
{
    using UnityEngine;
    using UnityEngine.Rendering.Universal;
    using CED = CoreEditorDrawer<UniversalRenderPipelineSerializedCamera>;

    static partial class UniversalRenderPipelineCameraUI
    {
        public partial class Environment
        {
            internal enum BackgroundType
            {
                Skybox = 0,
                SolidColor,
                [InspectorName("Uninitialized")]
                DontCare,
            }

            public static readonly CED.IDrawer Drawer = CED.FoldoutGroup(
                CameraUI.Environment.Styles.header,
                Expandable.Environment,
                k_ExpandedState,
                FoldoutOption.Indent,
                CED.Conditional(
                    (serialized, owner) => (CameraRenderType)serialized.cameraType.intValue == CameraRenderType.Base,
                    CED.Group(
                        Drawer_Environment_ClearFlags
                    )
                    ),
                CED.Group(
                    Styles.volumesSettingsText,
                    CED.Group(
                        GroupOption.Indent,
                        Drawer_Environment_VolumeUpdate,
                        CameraUI.Environment.Drawer_Environment_VolumeLayerMask,
                        Drawer_Environment_VolumeTrigger
                    )
                )
            );

            static BackgroundType GetBackgroundType(CameraClearFlags clearFlags)
            {
                switch (clearFlags)
                {
                    case CameraClearFlags.Skybox:
                        return BackgroundType.Skybox;
                    case CameraClearFlags.Nothing:
                        return BackgroundType.DontCare;

                    // DepthOnly is not supported by design in UniversalRP. We upgrade it to SolidColor
                    default:
                        return BackgroundType.SolidColor;
                }
            }

            static void Drawer_Environment_ClearFlags(UniversalRenderPipelineSerializedCamera p, Editor owner)
            {
                EditorGUI.showMixedValue = p.baseCameraSettings.clearFlags.hasMultipleDifferentValues;

                Rect clearFlagsRect = EditorGUILayout.GetControlRect();
                EditorGUI.BeginProperty(clearFlagsRect, Styles.backgroundType, p.baseCameraSettings.clearFlags);
                {
                    EditorGUI.BeginChangeCheck();
                    BackgroundType backgroundType = GetBackgroundType((CameraClearFlags)p.baseCameraSettings.clearFlags.intValue);
                    var selectedValue = (BackgroundType)EditorGUI.EnumPopup(clearFlagsRect, Styles.backgroundType, backgroundType);
                    if (EditorGUI.EndChangeCheck())
                    {
                        CameraClearFlags selectedClearFlags;
                        switch (selectedValue)
                        {
                            case BackgroundType.Skybox:
                                selectedClearFlags = CameraClearFlags.Skybox;
                                break;

                            case BackgroundType.DontCare:
                                selectedClearFlags = CameraClearFlags.Nothing;
                                break;

                            default:
                                selectedClearFlags = CameraClearFlags.SolidColor;
                                break;
                        }

                        p.baseCameraSettings.clearFlags.intValue = (int)selectedClearFlags;
                    }

                    if (!p.baseCameraSettings.clearFlags.hasMultipleDifferentValues)
                    {
                        if (GetBackgroundType((CameraClearFlags)p.baseCameraSettings.clearFlags.intValue) == BackgroundType.SolidColor)
                        {
                            using (var group = new EditorGUI.IndentLevelScope())
                            {
                                p.baseCameraSettings.DrawBackgroundColor();
                            }
                        }
                    }
                }
                EditorGUI.EndProperty();
                EditorGUI.showMixedValue = false;
            }

            static void Drawer_Environment_VolumeUpdate(UniversalRenderPipelineSerializedCamera p, Editor owner)
            {
                EditorGUI.BeginChangeCheck();
                VolumeFrameworkUpdateMode prevVolumeUpdateMode = (VolumeFrameworkUpdateMode)p.volumeFrameworkUpdateMode.intValue;
                EditorGUILayout.PropertyField(p.volumeFrameworkUpdateMode, Styles.volumeUpdates);
                if (EditorGUI.EndChangeCheck())
                {
                    if (p.serializedObject.targetObject is not Camera cam)
                        return;

                    VolumeFrameworkUpdateMode curVolumeUpdateMode = (VolumeFrameworkUpdateMode)p.volumeFrameworkUpdateMode.intValue;
                    cam.SetVolumeFrameworkUpdateMode(curVolumeUpdateMode);
                }
            }

            static void Drawer_Environment_VolumeTrigger(UniversalRenderPipelineSerializedCamera p, Editor owner)
            {
                var controlRect = EditorGUILayout.GetControlRect(true);
                EditorGUI.BeginProperty(controlRect, Styles.volumeTrigger, p.volumeTrigger);
                {
                    EditorGUI.BeginChangeCheck();
                    var newValue = EditorGUI.ObjectField(controlRect, Styles.volumeTrigger, (Transform)p.volumeTrigger.objectReferenceValue, typeof(Transform), true);
                    if (EditorGUI.EndChangeCheck() && !Equals(p.volumeTrigger.objectReferenceValue, newValue))
                        p.volumeTrigger.objectReferenceValue = newValue;
                }
                EditorGUI.EndProperty();
            }
        }
    }
}
