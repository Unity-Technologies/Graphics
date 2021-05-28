using UnityEngine;

namespace UnityEditor.Rendering.Universal
{
    static partial class UniversalRenderPipelineCameraUI
    {
        public partial class Output
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            public static GUIContent[] xrTargetEyeOptions =
            {
                EditorGUIUtility.TrTextContent("None"),
                EditorGUIUtility.TrTextContent("Both"),
            };
            public static int[] xrTargetEyeValues = { 0, 1 };
            public static readonly GUIContent xrTargetEye = EditorGUIUtility.TrTextContent("Target Eye", "Allows XR rendering if target eye sets to both eye. Disable XR for this camera otherwise.");
#endif

            public static string inspectorOverlayCameraText = L10n.Tr("Inspector Overlay Camera");
        }
    }
}
