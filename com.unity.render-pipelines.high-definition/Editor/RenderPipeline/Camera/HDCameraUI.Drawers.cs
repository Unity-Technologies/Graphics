using System.Linq;
using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    using CED = CoreEditorDrawer<SerializedHDCamera>;

    static partial class HDCameraUI
    {
        static readonly ExpandedState<CameraUI.Expandable, Camera> k_ExpandedState = new ExpandedState<CameraUI.Expandable, Camera>(CameraUI.Expandable.Projection, "HDRP");

        public static readonly CED.IDrawer SectionProjectionSettings = CED.FoldoutGroup(
            CameraUI.Styles.projectionSettingsHeaderContent,
            CameraUI.Expandable.Projection,
            k_ExpandedState,
            FoldoutOption.Indent,
            CED.Group(
                CameraUI.Drawer_Projection
                ),
            PhysicalCamera.Drawer,
            CED.Group(
                CameraUI.Drawer_FieldClippingPlanes
            )
        );

        public static readonly CED.IDrawer[] Inspector = new[]
        {
            SectionProjectionSettings,
            Rendering.Drawer,
            Environment.Drawer,
            Output.Drawer,
        };
    }
}
