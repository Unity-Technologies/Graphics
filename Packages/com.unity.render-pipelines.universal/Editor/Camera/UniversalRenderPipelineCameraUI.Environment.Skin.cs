using UnityEngine;

namespace UnityEditor.Rendering.Universal
{
    static partial class UniversalRenderPipelineCameraUI
    {
        public partial class Environment
        {
            public class Styles
            {
                public static GUIContent backgroundType = L10n.TextContent("Background Type", "Controls how to initialize the Camera's background.\n\nSkybox initializes camera with Skybox, defaulting to a background color if no skybox is found.\n\nSolid Color initializes background with the background color.\n\nUninitialized has undefined values for the camera background. Use this only if you are rendering all pixels in the Camera's view.", null, null);
                public static GUIContent volumesSettingsText = L10n.TextContent("Volumes", "These settings define how Volumes affect this Camera.", null, null);
                public static GUIContent volumeTrigger = L10n.TextContent("Volume Trigger", "A transform that will act as a trigger for volume blending. If none is set, the camera itself will act as a trigger.", null, null);
                public static GUIContent volumeUpdates = L10n.TextContent("Update Mode", "Select how Unity updates Volumes: every frame or when triggered via scripting. In the Editor, Unity updates Volumes every frame when not in the Play mode.", null, null);
            }
        }
    }
}
