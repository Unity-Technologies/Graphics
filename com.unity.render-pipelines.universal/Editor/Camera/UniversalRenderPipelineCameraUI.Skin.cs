using UnityEngine;

namespace UnityEditor.Rendering.Universal
{
    static partial class UniversalRenderPipelineCameraUI
    {
        public class Styles
        {
            public static GUIContent cameraType = EditorGUIUtility.TrTextContent("Render Type", "Defines if a camera renders directly to a target or overlays on top of another camera’s output. Overlay option is not available when Deferred Render Data is in use.");
            public static readonly string pixelPerfectInfo = L10n.Tr("Projection settings have been overriden by the Pixel Perfect Camera.");

            // Stack cameras
            public static GUIContent stackSettingsText = EditorGUIUtility.TrTextContent("Stack", "The list of overlay cameras assigned to this camera.");
            public static GUIContent cameras = EditorGUIUtility.TrTextContent("Cameras", "The list of overlay cameras assigned to this camera.");
            public static string inspectorOverlayCameraText = L10n.Tr("Inspector Overlay Camera");
        }
    }
}
