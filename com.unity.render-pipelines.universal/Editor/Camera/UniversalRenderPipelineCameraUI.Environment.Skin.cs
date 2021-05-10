using UnityEngine;

namespace UnityEditor.Rendering.Universal
{
    static partial class UniversalRenderPipelineCameraUI
    {
        public partial class Environment
        {
            public class Styles
            {
                public static GUIContent backgroundType = EditorGUIUtility.TrTextContent("Background Type", "Controls how to initialize the Camera's background.\n\nSkybox initializes camera with Skybox, defaulting to a background color if no skybox is found.\n\nSolid Color initializes background with the background color.\n\nUninitialized has undefined values for the camera background. Use this only if you are rendering all pixels in the Camera's view.");
                public static GUIContent volumeTrigger = EditorGUIUtility.TrTextContent("Volume Trigger", "A transform that will act as a trigger for volume blending. If none is set, the camera itself will act as a trigger.");
            }
        }
    }
}
