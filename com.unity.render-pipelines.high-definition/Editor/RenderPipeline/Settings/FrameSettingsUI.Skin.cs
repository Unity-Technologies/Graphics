using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.Events;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class FrameSettingsUI
    {
        const string renderingPassesHeaderContent = "Rendering Passes";
        const string renderingSettingsHeaderContent = "Rendering Settings";
        const string xrSettingsHeaderContent = "XR Settings";
        const string lightSettingsHeaderContent = "Lighting Settings";
        
        static readonly GUIContent transparentPrepassContent = CoreEditorUtils.GetContent("Enable Transparent Prepass");
        static readonly GUIContent transparentPostpassContent = CoreEditorUtils.GetContent("Enable Transparent Postpass");
        static readonly GUIContent motionVectorContent = CoreEditorUtils.GetContent("Enable Motion Vectors");
        static readonly GUIContent objectMotionVectorsContent = CoreEditorUtils.GetContent("Enable Object Motion Vectors");
        static readonly GUIContent decalsContent = CoreEditorUtils.GetContent("Enable Decals");
        static readonly GUIContent roughRefractionContent = CoreEditorUtils.GetContent("Enable Rough Refraction");
        static readonly GUIContent distortionContent = CoreEditorUtils.GetContent("Enable Distortion");
        static readonly GUIContent postprocessContent = CoreEditorUtils.GetContent("Enable Postprocess");
        static readonly GUIContent forwardRenderingOnlyContent = CoreEditorUtils.GetContent("Enable Forward Rendering Only");
        static readonly GUIContent depthPrepassWithDeferredRenderingContent = CoreEditorUtils.GetContent("Enable Depth Prepass With Deferred Rendering");
        static readonly GUIContent asyncComputeContent = CoreEditorUtils.GetContent("Enable Async Compute");
        static readonly GUIContent opaqueObjectsContent = CoreEditorUtils.GetContent("Enable Opaque Objects");
        static readonly GUIContent transparentObjectsContent = CoreEditorUtils.GetContent("Enable Transparent Objects");
        static readonly GUIContent msaaContent = CoreEditorUtils.GetContent("Enable MSAA");
        static readonly GUIContent stereoContent = CoreEditorUtils.GetContent("Enable Stereo");
        static readonly GUIContent xrGraphicConfigContent = CoreEditorUtils.GetContent("XR Graphics Config");
        static readonly GUIContent shadowContent = CoreEditorUtils.GetContent("Enable Shadow");
        static readonly GUIContent contactShadowContent = CoreEditorUtils.GetContent("Enable Contact Shadows");
        static readonly GUIContent shadowMaskContent = CoreEditorUtils.GetContent("Enable Shadow Masks");
        static readonly GUIContent ssrContent = CoreEditorUtils.GetContent("Enable SSR");
        static readonly GUIContent ssaoContent = CoreEditorUtils.GetContent("Enable SSAO");
        static readonly GUIContent subsurfaceScatteringContent = CoreEditorUtils.GetContent("Enable Subsurface Scattering");
        static readonly GUIContent transmissionContent = CoreEditorUtils.GetContent("Enable Transmission");
        static readonly GUIContent atmosphericScatteringContent = CoreEditorUtils.GetContent("Enable Atmospheric Scattering");
        static readonly GUIContent volumetricContent = CoreEditorUtils.GetContent("Enable Volumetric");
        static readonly GUIContent reprojectionForVolumetricsContent = CoreEditorUtils.GetContent("Enable Reprojection For Volumetrics");
        static readonly GUIContent lightLayerContent = CoreEditorUtils.GetContent("Enable LightLayers");
    }
}
