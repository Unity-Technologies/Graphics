using UnityEngine;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public sealed partial class HDRenderPipelineInspector
    {
        sealed class Styles
        {
            public readonly GUIContent defaults = new GUIContent("Defaults");
            public readonly GUIContent renderPipelineResources = new GUIContent("Render Pipeline Resources", "Set of resources that need to be loaded when creating stand alone");

            public readonly GUIContent settingsLabel = new GUIContent("Settings");

            public readonly GUIContent renderPipelineSettings = new GUIContent("Render Pipeline Settings");

            public readonly GUIContent supportDBuffer = new GUIContent("Support Decal buffer");
            public readonly GUIContent supportMSAA = new GUIContent("Support MSAA");
            // Shadow Settings
            public readonly GUIContent shadowSettings = new GUIContent("Shadow Settings");
            public readonly GUIContent shadowsAtlasWidth = new GUIContent("Atlas Width");
            public readonly GUIContent shadowsAtlasHeight = new GUIContent("Atlas Height");
            // LightLoop Settings
            public readonly GUIContent textureSettings = new GUIContent("LightLoop Settings");
            public readonly GUIContent spotCookieSize = new GUIContent("Spot Cookie Size");
            public readonly GUIContent pointCookieSize = new GUIContent("Point Cookie Size");
            public readonly GUIContent reflectionCubemapSize = new GUIContent("Reflection Cubemap Size");
            public readonly GUIContent reflectionCacheCompressed = new GUIContent("Compress Reflection Probe Cache");
            public readonly GUIContent skyReflectionSize = new GUIContent("Sky Reflection Size");
            public readonly GUIContent skyLightingOverride = new GUIContent("Sky Lighting Override Mask", "This layer mask will define in which layers the sky system will look for sky settings volumes for lighting override.");

            public readonly GUIContent defaultFrameSettings = new GUIContent("Default Frame Settings");

            // Rendering Settings
            public readonly GUIContent renderingSettingsLabel = new GUIContent("Rendering Settings");

            public readonly GUIContent diffusionProfileSettings = new GUIContent("Diffusion Profile Settings");
        }

        static Styles s_Styles;

        // Can't use a static initializer in case we need to create GUIStyle in the Styles class as
        // these can only be created with an active GUI rendering context
        void CheckStyles()
        {
            if (s_Styles == null)
                s_Styles = new Styles();
        }
    }
}
