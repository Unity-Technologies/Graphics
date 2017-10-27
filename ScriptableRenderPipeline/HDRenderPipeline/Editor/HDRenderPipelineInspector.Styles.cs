using UnityEngine;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public sealed partial class HDRenderPipelineInspector
    {
        // TODO: missing tooltips
        sealed class Styles
        {
            public readonly GUIContent defaults = new GUIContent("Defaults");
            public readonly GUIContent defaultDiffuseMaterial = new GUIContent("Default Diffuse Material", "Material to use when creating objects");
            public readonly GUIContent defaultShader = new GUIContent("Default Shader", "Shader to use when creating materials");

            public readonly GUIContent settingsLabel = new GUIContent("Settings");

            // Rendering Settings
            public readonly GUIContent renderingSettingsLabel = new GUIContent("Rendering Settings");
            public readonly GUIContent useForwardRenderingOnly = new GUIContent("Use Forward Rendering Only");
            public readonly GUIContent useDepthPrepassWithDeferredRendering = new GUIContent("Use Depth Prepass with Deferred rendering");
            public readonly GUIContent renderAlphaTestOnlyInDeferredPrepass = new GUIContent("Alpha Test Only");

            // Texture Settings
            public readonly GUIContent textureSettings = new GUIContent("Texture Settings");
            public readonly GUIContent spotCookieSize = new GUIContent("Spot Cookie Size");
            public readonly GUIContent pointCookieSize = new GUIContent("Point Cookie Size");
            public readonly GUIContent reflectionCubemapSize = new GUIContent("Reflection Cubemap Size");

            public readonly GUIContent sssSettings = new GUIContent("Subsurface Scattering Settings");

            // Shadow Settings
            public readonly GUIContent shadowSettings = new GUIContent("Shadow Settings");
            public readonly GUIContent shadowsAtlasWidth = new GUIContent("Atlas Width");
            public readonly GUIContent shadowsAtlasHeight = new GUIContent("Atlas Height");

            // Tile pass Settings
            public readonly GUIContent tileLightLoopSettings = new GUIContent("Tile Light Loop Settings");
            public readonly GUIContent enableTileAndCluster = new GUIContent("Tile/Clustered");
            public readonly GUIContent enableComputeLightEvaluation = new GUIContent("Compute Light Evaluation");
            public readonly GUIContent enableComputeLightVariants = new GUIContent("Compute Light Variants");
            public readonly GUIContent enableComputeMaterialVariants = new GUIContent("Compute Material Variants");
            public readonly GUIContent enableClustered = new GUIContent("Clustered");
            public readonly GUIContent enableFptlForOpaqueWhenClustered = new GUIContent("Fptl For Opaque When Clustered");
            public readonly GUIContent enableBigTilePrepass = new GUIContent("Big tile prepass");
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
