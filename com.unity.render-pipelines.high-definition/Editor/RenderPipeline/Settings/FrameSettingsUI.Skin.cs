using UnityEditor.Rendering;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class FrameSettingsUI
    {
        static readonly GUIContent frameSettingsHeaderContent = EditorGUIUtility.TrTextContent("Frame Settings Overrides", "Default FrameSettings are defined in your Unity Project's HDRP Asset.");

        const string renderingSettingsHeaderContent = "Rendering";
        const string lightSettingsHeaderContent = "Lighting";
        const string asyncComputeSettingsHeaderContent = "Async Compute";
        
        static readonly GUIContent transparentPrepassContent = EditorGUIUtility.TrTextContent("Transparent Prepass", "When enabled, HDRP processes a transparent prepass for Cameras using these Frame Settings.");
        static readonly GUIContent transparentPostpassContent = EditorGUIUtility.TrTextContent("Transparent Postpass", "When enabled, HDRP processes a transparent postpass for Cameras using these Frame Settings.");
        static readonly GUIContent motionVectorContent = EditorGUIUtility.TrTextContent("Motion Vectors", "When enabled, HDRP processes a motion vector pass for Cameras using these Frame Settings.");
        static readonly GUIContent objectMotionVectorsContent = EditorGUIUtility.TrTextContent("Object Motion Vectors", "When enabled, HDRP processes an object motion vector pass for Cameras using these Frame Settings.");
        static readonly GUIContent decalsContent = EditorGUIUtility.TrTextContent("Decals", "When enabled, HDRP processes a decal render pass for Cameras using these Frame Settings.");
        static readonly GUIContent roughRefractionContent = EditorGUIUtility.TrTextContent("Rough Refraction", "When enabled, HDRP processes a rough refraction render pass for Cameras using these Frame Settings.");
        static readonly GUIContent distortionContent = EditorGUIUtility.TrTextContent("Distortion", "When enabled, HDRP processes a distortion render pass for Cameras using these Frame Settings.");
        static readonly GUIContent postprocessContent = EditorGUIUtility.TrTextContent("Postprocess", "When enabled, HDRP processes a postprocessing render pass for Cameras using these Frame Settings.");
        static readonly GUIContent colorBufferFormat = EditorGUIUtility.TrTextContent("Color Buffer Format", "Specify the color format for the color buffer.");
        static readonly GUIContent litShaderModeContent = EditorGUIUtility.TrTextContent("Lit Shader Mode", "Specifies the Lit Shader Mode Cameras using these Frame Settings use to render the Scene.");
        static readonly GUIContent depthPrepassWithDeferredRenderingContent = EditorGUIUtility.TrTextContent("Depth Prepass With Deferred Rendering", "When enabled, HDRP processes a depth prepass for Cameras using these Frame Settings. Set Lit Shader Mode to Deferred to access this option.");
        static readonly GUIContent opaqueObjectsContent = EditorGUIUtility.TrTextContent("Opaque Objects", "When enabled, Cameras using these Frame Settings render opaque GameObjects.");
        static readonly GUIContent transparentObjectsContent = EditorGUIUtility.TrTextContent("Transparent Objects", "When enabled, Cameras using these Frame Settings render Transparent GameObjects.");
        static readonly GUIContent realtimePlanarReflectionContent = EditorGUIUtility.TrTextContent("Enable Realtime Planar Reflection", "When enabled, HDRP updates Planar Reflection Probes every frame for Cameras using these Frame Settings.");
        static readonly GUIContent msaaContent = EditorGUIUtility.TrTextContent("MSAA", "When enabled, Cameras using these Frame Settings calculate MSAA when they render the Scene. Set Lit Shader Mode to Forward to access this option.");
        static readonly GUIContent shadowContent = EditorGUIUtility.TrTextContent("Shadow", "When enabled, Cameras using these Frame Settings render shadows.");
        static readonly GUIContent contactShadowContent = EditorGUIUtility.TrTextContent("Contact Shadows", "When enabled, Cameras using these Frame Settings render Contact Shadows.");
        static readonly GUIContent shadowMaskContent = EditorGUIUtility.TrTextContent("Shadow Masks", "When enabled, Cameras using these Frame Settings render shadows from Shadow Masks.");
        static readonly GUIContent ssrContent = EditorGUIUtility.TrTextContent("SSR", "When enabled, Cameras using these Frame Settings calculate Screen Space Reflections.");
        static readonly GUIContent ssaoContent = EditorGUIUtility.TrTextContent("SSAO", "When enabled, Cameras using these Frame Settings calculate Screen Space Ambient Occlusion.");
        static readonly GUIContent subsurfaceScatteringContent = EditorGUIUtility.TrTextContent("Subsurface Scattering", "When enabled, Cameras using these Frame Settings render subsurface scattering (SSS) effects for GameObjects that use a SSS Material.");
        static readonly GUIContent transmissionContent = EditorGUIUtility.TrTextContent("Transmission", "When enabled, Cameras using these Frame Settings render subsurface scattering (SSS) Materials with an added transmission effect (only if you enable Transmission on the the SSS Material in the Material's Inspector).");
        static readonly GUIContent atmosphericScatteringContent = EditorGUIUtility.TrTextContent("Atmospheric Scattering", "When enabled, Cameras using these Frame Settings render atmospheric scattering effects such as fog.");
        static readonly GUIContent volumetricContent = EditorGUIUtility.TrTextContent("Volumetrics", "When enabled, Cameras using these Frame Settings render volumetric effects such as volumetric fog and lighting.");
        static readonly GUIContent reprojectionForVolumetricsContent = EditorGUIUtility.TrTextContent("Reprojection For Volumetrics", "When enabled, Cameras using these Frame Settings use several previous frames to calculate volumetric effects which increases their overall quality at run time.");
        static readonly GUIContent lightLayerContent = EditorGUIUtility.TrTextContent("LightLayers", "When enabled, Cameras that use these Frame Settings make use of LightLayers.");

        // Async compute
        static readonly GUIContent asyncComputeContent = EditorGUIUtility.TrTextContent("Async Compute", "When enabled, HDRP executes certain compute Shader commands in parallel. This only has an effect if the target platform supports async compute.");
        static readonly GUIContent lightListAsyncContent = EditorGUIUtility.TrTextContent("Build Light List in Async", "When enabled, HDRP builds the Light List asynchronously.");
        static readonly GUIContent SSRAsyncContent = EditorGUIUtility.TrTextContent("SSR in Async", "When enabled, HDRP calculates screen space reflection asynchronously.");
        static readonly GUIContent SSAOAsyncContent = EditorGUIUtility.TrTextContent("SSAO in Async", "When enabled, HDRP calculates screen space ambient occlusion asynchronously.");
        static readonly GUIContent contactShadowsAsyncContent = EditorGUIUtility.TrTextContent("Contact Shadows in Async", "When enabled, HDRP calculates Contact Shadows asynchronously.");
        static readonly GUIContent volumeVoxelizationAsyncContent = EditorGUIUtility.TrTextContent("Volumetrics Voxelization in Async", "When enabled, HDRP calculates volumetric voxelization asynchronously.");

        const string lightLoopSettingsHeaderContent = "Light Loop";


        static readonly GUIContent exposureControlContent = EditorGUIUtility.TrTextContent("Exposure Control");


       

        
        // Light Loop
        // Uncomment if you re-enable LIGHTLOOP_SINGLE_PASS multi_compile in lit*.shader
        //static readonly GUIContent tileAndClusterContent = EditorGUIUtility.TrTextContent("Enable Tile And Cluster");
        static readonly GUIContent fptlForForwardOpaqueContent = EditorGUIUtility.TrTextContent("FPTL For Forward Opaque");
        static readonly GUIContent bigTilePrepassContent = EditorGUIUtility.TrTextContent("Big Tile Prepass");
        static readonly GUIContent computeLightEvaluationContent = EditorGUIUtility.TrTextContent("Compute Light Evaluation");
        static readonly GUIContent computeLightVariantsContent = EditorGUIUtility.TrTextContent("Compute Light Variants");
        static readonly GUIContent computeMaterialVariantsContent = EditorGUIUtility.TrTextContent("Compute Material Variants");
    }
}
