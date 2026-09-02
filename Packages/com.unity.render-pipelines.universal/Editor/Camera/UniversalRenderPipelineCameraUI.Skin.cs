using UnityEngine;

namespace UnityEditor.Rendering.Universal
{
    static partial class UniversalRenderPipelineCameraUI
    {
        public class Styles
        {
            public static readonly GUIContent cameraType = L10n.TextContent("Render Type", "Defines if a camera renders directly to a target or overlays on top of another camera’s output. Overlay option is not available when Deferred Render Data is in use.", null, null);
            public static readonly string pixelPerfectInfo = L10n.Tr("Projection settings have been overriden by the Pixel Perfect Camera.", null);

            // Stack cameras
            public static readonly GUIContent stackSettingsText = L10n.TextContent("Stack", "The list of overlay cameras assigned to this camera.", null, null);
            public static readonly GUIContent cameras = L10n.TextContent("Cameras", "The list of overlay cameras assigned to this camera.", null, null);
            public static readonly string inspectorOverlayCameraText = L10n.Tr("Inspector Overlay Camera", null);
            
            public static readonly string formatterTileOnlyMode = L10n.Tr("'{0}' will be skipped because it is incompatible with the enabled 'Tile-Only Mode' on the assigned Renderer: {1}.", null);
            public static readonly GUIContent cameraStackLabelForTileOnlyMode = L10n.TextContent("Camera Stacking", null, null, null);
        }
    }
}
