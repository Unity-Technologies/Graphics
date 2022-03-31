namespace UnityEditor.Rendering.HighDefinition
{
    using CED = CoreEditorDrawer<SerializedHDCamera>;

    static partial class HDCameraUI
    {
        partial class PhysicalCamera
        {
            public static readonly CED.IDrawer Drawer = CED.Conditional(
                (serialized, owner) => serialized.projectionMatrixMode.intValue == (int)CameraUI.ProjectionMatrixMode.PhysicalPropertiesBased,
                CED.Group(
                    CameraUI.PhysicalCamera.Styles.cameraBody,
                    GroupOption.Indent,
                    CED.Group(
                        GroupOption.Indent,
                        CameraUI.PhysicalCamera.Drawer_PhysicalCamera_CameraBody_Sensor,
                        CameraUI.PhysicalCamera.Drawer_PhysicalCamera_CameraBody_ISO,
                        CameraUI.PhysicalCamera.Drawer_PhysicalCamera_CameraBody_ShutterSpeed,
                        CameraUI.PhysicalCamera.Drawer_PhysicalCamera_CameraBody_GateFit
                    )
                    ),
                CED.Group(
                    CameraUI.PhysicalCamera.Styles.lens,
                    GroupOption.Indent,
                    CED.Group(
                        GroupOption.Indent,
                        CameraUI.PhysicalCamera.Drawer_PhysicalCamera_Lens_FocalLength,
                        CameraUI.PhysicalCamera.Drawer_PhysicalCamera_Lens_Shift,
                        CameraUI.PhysicalCamera.Drawer_PhysicalCamera_Lens_Aperture,
                        CameraUI.PhysicalCamera.Drawer_PhysicalCamera_FocusDistance
                    )
                    ),
                CED.Group(
                    CameraUI.PhysicalCamera.Styles.apertureShape,
                    GroupOption.Indent,
                    CED.Group(
                        GroupOption.Indent,
                        CameraUI.PhysicalCamera.Drawer_PhysicalCamera_ApertureShape
                    )
                )
            );

            public static readonly CED.IDrawer DrawerPreset = CED.Conditional(
                (serialized, owner) => serialized.projectionMatrixMode.intValue == (int)CameraUI.ProjectionMatrixMode.PhysicalPropertiesBased,
                CED.Group(
                    CameraUI.PhysicalCamera.Styles.cameraBody,
                    GroupOption.Indent,
                    CED.Group(
                        GroupOption.Indent,
                        CameraUI.PhysicalCamera.Drawer_PhysicalCamera_CameraBody_Sensor,
                        CameraUI.PhysicalCamera.Drawer_PhysicalCamera_CameraBody_GateFit
                    )
                    ),
                CED.Group(
                    CameraUI.PhysicalCamera.Styles.lens,
                    GroupOption.Indent,
                    CED.Group(
                        GroupOption.Indent,
                        CameraUI.PhysicalCamera.Drawer_PhysicalCamera_Lens_FocalLength,
                        CameraUI.PhysicalCamera.Drawer_PhysicalCamera_Lens_Shift
                    )
                )
            );
        }
    }
}
