using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    using CED = CoreEditorDrawer<UniversalRenderPipelineSerializedCamera>;

    static partial class UniversalRenderPipelineCameraUI
    {
        static readonly ExpandedState<Expandable, Camera> k_ExpandedStatePreset = new(0, "URP-preset");

        public static readonly CED.IDrawer PresetInspector = CED.Group(
            CED.Group((serialized, owner) =>
                EditorGUILayout.HelpBox(CameraUI.Styles.unsupportedPresetPropertiesMessage, MessageType.Info)),
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            CED.FoldoutGroup(
                CameraUI.Styles.projectionSettingsHeaderContent,
                Expandable.Projection,
                k_ExpandedStatePreset,
                FoldoutOption.Indent,
                CED.Group(
                    CameraUI.Drawer_Projection),
                PhysicalCamera.Drawer),
            Rendering.DrawerPreset
        );
    }
}
