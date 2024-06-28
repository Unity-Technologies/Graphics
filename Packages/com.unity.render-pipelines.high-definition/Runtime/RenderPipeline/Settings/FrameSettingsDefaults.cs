using System;

namespace UnityEngine.Rendering.HighDefinition
{
    static class FrameSettingsDefaults
    {
        private static uint[] s_CameraDefaultbitDatas = new uint[]
        {
            (uint)FrameSettingsField.ShadowMaps,
            (uint)FrameSettingsField.ContactShadows,
            (uint)FrameSettingsField.Shadowmask,
            (uint)FrameSettingsField.ScreenSpaceShadows,
            (uint)FrameSettingsField.SSR,
            (uint)FrameSettingsField.TransparentSSR,
            (uint)FrameSettingsField.SSAO,
            (uint)FrameSettingsField.SSGI,
            (uint)FrameSettingsField.SubsurfaceScattering,
            (uint)FrameSettingsField
                .Transmission, // Caution: this is only for debug, it doesn't save the cost of Transmission execution
            (uint)FrameSettingsField.AtmosphericScattering,
            (uint)FrameSettingsField.Volumetrics,
            (uint)FrameSettingsField.ReprojectionForVolumetrics,
            (uint)FrameSettingsField.LightLayers,
            (uint)FrameSettingsField.ExposureControl,
            (uint)FrameSettingsField.LitShaderMode, //deffered ; enum with only two value saved as a bool
            (uint)FrameSettingsField.TransparentPrepass,
            (uint)FrameSettingsField.TransparentPostpass,
            (uint)FrameSettingsField.CustomPass,
            (uint)FrameSettingsField.VirtualTexturing,
            (uint)FrameSettingsField.MotionVectors, // Enable/disable whole motion vectors pass (Camera + Object).
            (uint)FrameSettingsField.ObjectMotionVectors,
            (uint)FrameSettingsField.RenderingLayerMaskBuffer,
            (uint)FrameSettingsField.Decals,
            (uint)FrameSettingsField.DecalLayers,
            (uint)FrameSettingsField
                .Refraction, // Depends on DepthPyramid - If not enable, just do a copy of the scene color (?) - how to disable refraction ?
            (uint)FrameSettingsField.Distortion,
            (uint)FrameSettingsField.RoughDistortion,
            (uint)FrameSettingsField.Postprocess,
            (uint)FrameSettingsField.CustomPostProcess,
            (uint)FrameSettingsField.StopNaN,
            (uint)FrameSettingsField.DepthOfField,
            (uint)FrameSettingsField.MotionBlur,
            (uint)FrameSettingsField.PaniniProjection,
            (uint)FrameSettingsField.Bloom,
            (uint)FrameSettingsField.LensFlareDataDriven,
            (uint)FrameSettingsField.LensDistortion,
            (uint)FrameSettingsField.LensFlareScreenSpace,
            (uint)FrameSettingsField.ChromaticAberration,
            (uint)FrameSettingsField.Vignette,
            (uint)FrameSettingsField.ColorGrading,
            (uint)FrameSettingsField.Tonemapping,
            (uint)FrameSettingsField.FilmGrain,
            (uint)FrameSettingsField.Dithering,
            (uint)FrameSettingsField.Antialiasing,
            (uint)FrameSettingsField.AfterPostprocess,
            (uint)FrameSettingsField.LowResTransparent,
            (uint)FrameSettingsField.ZTestAfterPostProcessTAA,
            (uint)FrameSettingsField.OpaqueObjects,
            (uint)FrameSettingsField.TransparentObjects,
            (uint)FrameSettingsField.AsyncCompute,
            (uint)FrameSettingsField.LightListAsync,
            (uint)FrameSettingsField.SSRAsync,
            (uint)FrameSettingsField.SSRAsync,
            (uint)FrameSettingsField.SSAOAsync,
            (uint)FrameSettingsField.ContactShadowsAsync,
            (uint)FrameSettingsField.VolumeVoxelizationsAsync,
            (uint)FrameSettingsField.HighQualityLineRendering,
            (uint)FrameSettingsField.HighQualityLinesAsync,
            (uint)FrameSettingsField.ComputeLightVariants,
            (uint)FrameSettingsField.ComputeMaterialVariants,
            (uint)FrameSettingsField.FPTLForForwardOpaque,
            (uint)FrameSettingsField.BigTilePrepass,
            (uint)FrameSettingsField.TransparentsWriteMotionVector,
            (uint)FrameSettingsField.ReflectionProbe,
            (uint)FrameSettingsField.PlanarProbe,
            (uint)FrameSettingsField.SkyReflection,
            (uint)FrameSettingsField.DirectSpecularLighting,
            (uint)FrameSettingsField.RayTracing,
            (uint)FrameSettingsField.RaytracingVFX,
            (uint)FrameSettingsField.AdaptiveProbeVolume,
            (uint)FrameSettingsField.VolumetricClouds,
            (uint)FrameSettingsField.Water,

            (uint)FrameSettingsField.WaterDecals,
            (uint)FrameSettingsField.WaterExclusion,
            (uint)FrameSettingsField.ComputeThickness
            // (uint)FullResolutionCloudsForSky
        };

        private static uint[] s_BakedOrCustomReflectionbitDatas = new uint[]
        {
            (uint)FrameSettingsField.ShadowMaps,
            //(uint)FrameSettingsField.ContactShadow,
            //(uint)FrameSettingsField.ShadowMask,
            //(uint)FrameSettingsField.SSR,
            //(uint)FrameSettingsField.SSAO,
            //(uint)FrameSettingsField.SSGI,
            (uint)FrameSettingsField.SubsurfaceScattering,
            (uint)FrameSettingsField
                .Transmission, // Caution: this is only for debug, it doesn't save the cost of Transmission execution
            //(uint)FrameSettingsField.AtmosphericScaterring,
            (uint)FrameSettingsField.Volumetrics,
            (uint)FrameSettingsField.ReprojectionForVolumetrics,
            (uint)FrameSettingsField.LightLayers,
            //(uint)FrameSettingsField.ExposureControl,
            (uint)FrameSettingsField.LitShaderMode, //deffered ; enum with only two value saved as a bool
            (uint)FrameSettingsField.TransparentPrepass,
            (uint)FrameSettingsField.TransparentPostpass,
            (uint)FrameSettingsField.CustomPass,
            (uint)FrameSettingsField.VirtualTexturing,
            (uint)FrameSettingsField.MotionVectors, // Enable/disable whole motion vectors pass (Camera + Object).
            (uint)FrameSettingsField.ObjectMotionVectors,
            (uint)FrameSettingsField.Decals,
            (uint)FrameSettingsField.DecalLayers,
            //(uint)FrameSettingsField.Refraction, // Depends on DepthPyramid - If not enable, just do a copy of the scene color (?) - how to disable refraction ?
            //(uint)FrameSettingsField.Distortion,
            //(uint)FrameSettingsField.RoughDistortion,
            //(uint)FrameSettingsField.Postprocess,
            //(uint)FrameSettingsField.CustomPostProcess,
            //(uint)FrameSettingsField.AfterPostprocess,
            (uint)FrameSettingsField.OpaqueObjects,
            (uint)FrameSettingsField.TransparentObjects,
            (uint)FrameSettingsField.AsyncCompute,
            (uint)FrameSettingsField.LightListAsync,
            (uint)FrameSettingsField.SSRAsync,
            (uint)FrameSettingsField.SSRAsync,
            (uint)FrameSettingsField.SSAOAsync,
            (uint)FrameSettingsField.ContactShadowsAsync,
            (uint)FrameSettingsField.VolumeVoxelizationsAsync,
            (uint)FrameSettingsField.HighQualityLinesAsync,
            (uint)FrameSettingsField.ComputeLightVariants,
            (uint)FrameSettingsField.ComputeMaterialVariants,
            (uint)FrameSettingsField.FPTLForForwardOpaque,
            (uint)FrameSettingsField.BigTilePrepass,
            (uint)FrameSettingsField.ReflectionProbe,
            (uint)FrameSettingsField.RayTracing,
            (uint)FrameSettingsField.RaytracingVFX,
            // (uint)FrameSettingsField.EnableSkyReflection,
            (uint)FrameSettingsField.AdaptiveProbeVolume,
            (uint)FrameSettingsField.DirectSpecularLighting,
            // (uint)FrameSettingsField.VolumetricClouds,
            // (uint)FrameSettingsField.Water,
            // (uint)FrameSettingsField.WaterExclusion,
            // (uint)FullResolutionCloudsForSky
        };

        private static uint[] s_RealtimeReflectionbitDatas = new uint[]
        {
            (uint)FrameSettingsField.ShadowMaps,
            (uint)FrameSettingsField.ContactShadows,
            (uint)FrameSettingsField.Shadowmask,
            //(uint)FrameSettingsField.SSR,
            (uint)FrameSettingsField.SSAO,
            //(uint)FrameSettingsField.SSGI,
            (uint)FrameSettingsField.SubsurfaceScattering,
            (uint)FrameSettingsField
                .Transmission, // Caution: this is only for debug, it doesn't save the cost of Transmission execution
            (uint)FrameSettingsField.AtmosphericScattering,
            (uint)FrameSettingsField.Volumetrics,
            (uint)FrameSettingsField.ReprojectionForVolumetrics,
            (uint)FrameSettingsField.LightLayers,
            //(uint)FrameSettingsField.ExposureControl,
            (uint)FrameSettingsField.LitShaderMode, //deffered ; enum with only two value saved as a bool
            (uint)FrameSettingsField.TransparentPrepass,
            (uint)FrameSettingsField.TransparentPostpass,
            (uint)FrameSettingsField.CustomPass,
            (uint)FrameSettingsField.VirtualTexturing,
            //(uint)FrameSettingsField.MotionVectors, // Enable/disable whole motion vectors pass (Camera + Object).
            //(uint)FrameSettingsField.ObjectMotionVectors,
            (uint)FrameSettingsField.Decals,
            (uint)FrameSettingsField.DecalLayers,
            (uint)FrameSettingsField
                .Refraction, // Depends on DepthPyramid - If not enable, just do a copy of the scene color (?) - how to disable rough refraction ?
            (uint)FrameSettingsField.Distortion,
            (uint)FrameSettingsField.RoughDistortion,
            //(uint)FrameSettingsField.Postprocess,
            //(uint)FrameSettingsField.CustomPostProcess,
            //(uint)FrameSettingsField.AfterPostprocess,
            (uint)FrameSettingsField.OpaqueObjects,
            (uint)FrameSettingsField.TransparentObjects,
            (uint)FrameSettingsField.AsyncCompute,
            (uint)FrameSettingsField.LightListAsync,
            //(uint)FrameSettingsField.SSRAsync,
            (uint)FrameSettingsField.SSAOAsync,
            (uint)FrameSettingsField.ContactShadowsAsync,
            (uint)FrameSettingsField.VolumeVoxelizationsAsync,
            (uint)FrameSettingsField.HighQualityLineRendering,
            (uint)FrameSettingsField.HighQualityLinesAsync,
            (uint)FrameSettingsField.ComputeLightVariants,
            (uint)FrameSettingsField.ComputeMaterialVariants,
            (uint)FrameSettingsField.FPTLForForwardOpaque,
            (uint)FrameSettingsField.BigTilePrepass,
            (uint)FrameSettingsField.ReplaceDiffuseForIndirect,
            // (uint)FrameSettingsField.EnableSkyReflection,
            // (uint)FrameSettingsField.DirectSpecularLighting,
            (uint)FrameSettingsField.VolumetricClouds,
            (uint)FrameSettingsField.Water,

            (uint)FrameSettingsField.WaterDecals,
            (uint)FrameSettingsField.WaterExclusion,
            (uint)FrameSettingsField.AdaptiveProbeVolume,
            // (uint)FullResolutionCloudsForSky
        };

        static uint[] GetFrameSettingsRenderTypeBitDatas(FrameSettingsRenderType defaultFrameSettingsRenderType)
        {
            switch (defaultFrameSettingsRenderType)
            {
                case FrameSettingsRenderType.Camera:
                    return s_CameraDefaultbitDatas;
                case FrameSettingsRenderType.CustomOrBakedReflection:
                    return s_BakedOrCustomReflectionbitDatas;
                case FrameSettingsRenderType.RealtimeReflection:
                    return s_RealtimeReflectionbitDatas;
                default:
                    throw new ArgumentException($"Unhandled {nameof(FrameSettingsRenderType)} defaults");
            }
        }

        public static FrameSettings Get(FrameSettingsRenderType defaultFrameSettingsRenderType)
        {
            return new FrameSettings()
            {
                bitDatas = new BitArray128(GetFrameSettingsRenderTypeBitDatas(defaultFrameSettingsRenderType)),
                lodBias = 1,
                sssQualityMode = SssQualityMode.FromQualitySettings,
                sssQualityLevel = 0,
                sssCustomSampleBudget = (int)DefaultSssSampleBudgetForQualityLevel.Low,
                sssCustomDownsampleSteps = (int)DefaultSssDownsampleSteps.Low,
                msaaMode = MSAAMode.None,
            };
        }
    }
}
