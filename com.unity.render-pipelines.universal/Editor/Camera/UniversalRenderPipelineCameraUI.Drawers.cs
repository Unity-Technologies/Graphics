using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    using CED = CoreEditorDrawer<UniversalRenderPipelineSerializedCamera>;

    static partial class UniversalRenderPipelineCameraUI
    {
        static readonly ExpandedState<CameraUI.Expandable, Camera> k_ExpandedState = new(CameraUI.Expandable.Projection, "URP");

        public static readonly CED.IDrawer SectionProjectionSettings = CED.FoldoutGroup(
            CameraUI.Styles.projectionSettingsHeaderContent,
            CameraUI.Expandable.Projection,
            k_ExpandedState,
            FoldoutOption.Indent,
            CED.Group(
                DrawerProjection
                ),
            PhysicalCamera.Drawer,
            CED.Group(
                CameraUI.Drawer_FieldClippingPlanes
            )
        );

        public static readonly CED.IDrawer SectionStackSettings =
            CED.Conditional(
                (serialized, editor) => (CameraRenderType)serialized.cameraType.intValue == CameraRenderType.Base,
                CED.FoldoutGroup(Styles.stackSettingsText, CameraUI.Expandable.Stack, k_ExpandedState, FoldoutOption.Indent, CED.Group(DrawerStackCameras)));

        public static readonly CED.IDrawer[] Inspector =
        {
            CED.Group(
                DrawerCameraType
                ),
            SectionProjectionSettings,
            Rendering.Drawer,
            SectionStackSettings,
            Environment.Drawer,
            Output.Drawer
        };

        static void DrawerProjection(UniversalRenderPipelineSerializedCamera p, Editor owner)
        {
            var camera = p.serializedObject.targetObject as Camera;
            bool pixelPerfectEnabled = camera.TryGetComponent<UnityEngine.Experimental.Rendering.Universal.PixelPerfectCamera>(out var pixelPerfectCamera) && pixelPerfectCamera.enabled;
            if (pixelPerfectEnabled)
                EditorGUILayout.HelpBox(Styles.pixelPerfectInfo, MessageType.Info);

            using (new EditorGUI.DisabledGroupScope(pixelPerfectEnabled))
                CameraUI.Drawer_Projection(p, owner);
        }

        static void DrawerCameraType(UniversalRenderPipelineSerializedCamera p, Editor owner)
        {
            int selectedRenderer = p.renderer.intValue;
            ScriptableRenderer scriptableRenderer = UniversalRenderPipeline.asset.GetRenderer(selectedRenderer);
            bool isDeferred = scriptableRenderer is UniversalRenderer {renderingMode : RenderingMode.Deferred};

            EditorGUI.BeginChangeCheck();

            CameraRenderType originalCamType = (CameraRenderType)p.cameraType.intValue;
            CameraRenderType camType = (originalCamType != CameraRenderType.Base && isDeferred) ? CameraRenderType.Base : originalCamType;

            camType = (CameraRenderType)EditorGUILayout.EnumPopup(
                Styles.cameraType,
                camType,
                e => !isDeferred || (CameraRenderType)e != CameraRenderType.Overlay,
                false
            );

            if (EditorGUI.EndChangeCheck() || camType != originalCamType)
            {
                p.cameraType.intValue = (int)camType;
            }

            EditorGUILayout.Space();
        }

        static void DrawerStackCameras(UniversalRenderPipelineSerializedCamera p, Editor owner)
        {
            if (owner is UniversalRenderPipelineCameraEditor cameraEditor)
            {
                cameraEditor.DrawStackSettings();
            }
        }
    }
}
