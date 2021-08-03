using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    using CED = CoreEditorDrawer<SerializedHDCamera>;

    static partial class HDCameraUI
    {
        static readonly ExpandedState<Expandable, Camera> k_ExpandedStatePreset = new(Expandable.Projection, "HDRP-preset");

        public static readonly CED.IDrawer PresetInspector = CED.Group(
            CED.Group((serialized, owner) =>
                EditorGUILayout.HelpBox(CameraUI.Styles.unsupportedPresetPropertiesMessage, MessageType.Info)),
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            CED.FoldoutGroup(
                CameraUI.Styles.projectionSettingsHeaderContent,
                Expandable.Projection,
                k_ExpandedStatePreset,
                FoldoutOption.Indent,
                CED.Group(CameraUI.Drawer_Projection),
                PhysicalCamera.DrawerPreset
                ),
            Rendering.DrawerPreset
        );
    }
}
