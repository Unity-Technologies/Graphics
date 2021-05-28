namespace UnityEditor.Rendering.Universal
{
    using CED = CoreEditorDrawer<UniversalRenderPipelineSerializedCamera>;

    static partial class UniversalRenderPipelineCameraUI
    {
        public partial class PhysicalCamera
        {
            public static readonly CED.IDrawer Drawer = CED.Conditional(
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
