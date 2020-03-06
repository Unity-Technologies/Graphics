using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    [Obsolete("For data migration")]
    enum ObsoleteLitShaderMode
    {
        Forward,
        Deferred
    }

    [Flags, Obsolete("For data migration")]
    enum ObsoleteLightLoopSettingsOverrides
    {
        FptlForForwardOpaque = 1 << 0,
        BigTilePrepass = 1 << 1,
        ComputeLightEvaluation = 1 << 2,
        ComputeLightVariants = 1 << 3,
        ComputeMaterialVariants = 1 << 4,
        TileAndCluster = 1 << 5,
        //Fptl = 1 << 6, //isFptlEnabled set up by system
    }

    [Flags, Obsolete("For data migration")]
    enum ObsoleteFrameSettingsOverrides
    {
        //lighting settings
        Shadow = 1 << 0,
        ContactShadow = 1 << 1,
        ShadowMask = 1 << 2,
        SSR = 1 << 3,
        SSAO = 1 << 4,
        SubsurfaceScattering = 1 << 5,
        Transmission = 1 << 6,
        AtmosphericScaterring = 1 << 7,
        Volumetrics = 1 << 8,
        ReprojectionForVolumetrics = 1 << 9,
        LightLayers = 1 << 10,
        MSAA = 1 << 11,
        ExposureControl = 1 << 12,

        //rendering pass
        TransparentPrepass = 1 << 13,
        TransparentPostpass = 1 << 14,
        MotionVectors = 1 << 15,
        ObjectMotionVectors = 1 << 16,
        Decals = 1 << 17,
        RoughRefraction = 1 << 18,
        Distortion = 1 << 19,
        Postprocess = 1 << 20,

        //rendering settings
        ShaderLitMode = 1 << 21,
        DepthPrepassWithDeferredRendering = 1 << 22,
        OpaqueObjects = 1 << 24,
        TransparentObjects = 1 << 25,

        // Async settings
        AsyncCompute = 1 << 23,
        LightListAsync = 1 << 27,
        SSRAsync = 1 << 28,
        SSAOAsync = 1 << 29,
        ContactShadowsAsync = 1 << 30,
        VolumeVoxelizationsAsync = 1 << 31,
    }

    [Serializable, Obsolete("For data migration")]
    class ObsoleteLightLoopSettings
    {
        public ObsoleteLightLoopSettingsOverrides overrides;
        [FormerlySerializedAs("enableTileAndCluster")]
        public bool enableDeferredTileAndCluster;
        public bool enableComputeLightEvaluation;
        public bool enableComputeLightVariants;
        public bool enableComputeMaterialVariants;
        public bool enableFptlForForwardOpaque;
        public bool enableBigTilePrepass;
        public bool isFptlEnabled;
    }

    // The settings here are per frame settings.
    // Each camera must have its own per frame settings
    [Serializable]
    [System.Diagnostics.DebuggerDisplay("FrameSettings overriding {overrides.ToString(\"X\")}")]
    [Obsolete("For data migration")]
    class ObsoleteFrameSettings
    {
        public ObsoleteFrameSettingsOverrides overrides;

        public bool enableShadow;
        public bool enableContactShadows;
        public bool enableShadowMask;
        public bool enableSSR;
        public bool enableSSAO;
        public bool enableSubsurfaceScattering;
        public bool enableTransmission;
        public bool enableAtmosphericScattering;
        public bool enableVolumetrics;
        public bool enableReprojectionForVolumetrics;
        public bool enableLightLayers;
        public bool enableExposureControl = true;

        public float diffuseGlobalDimmer;
        public float specularGlobalDimmer;

        public ObsoleteLitShaderMode shaderLitMode;
        public bool enableDepthPrepassWithDeferredRendering;

        public bool enableTransparentPrepass;
        public bool enableMotionVectors; // Enable/disable whole motion vectors pass (Camera + Object).
        public bool enableObjectMotionVectors;
        [FormerlySerializedAs("enableDBuffer")]
        public bool enableDecals;
        public bool enableRoughRefraction; // Depends on DepthPyramid - If not enable, just do a copy of the scene color (?) - how to disable rough refraction ?
        public bool enableTransparentPostpass;
        public bool enableDistortion;
        public bool enablePostprocess;

        public bool enableOpaqueObjects;
        public bool enableTransparentObjects;
        public bool enableRealtimePlanarReflection;

        public bool enableMSAA;

        public bool enableAsyncCompute;
        public bool runLightListAsync;
        public bool runSSRAsync;
        public bool runSSAOAsync;
        public bool runContactShadowsAsync;
        public bool runVolumeVoxelizationAsync;

        public ObsoleteLightLoopSettings lightLoopSettings;
    }

    public partial struct FrameSettings
    {
#pragma warning disable 618 // Type or member is obsolete
        internal static void MigrateFromClassVersion(ref ObsoleteFrameSettings oldFrameSettingsFormat, ref FrameSettings newFrameSettingsFormat, ref FrameSettingsOverrideMask newFrameSettingsOverrideMask)
        {
            if (oldFrameSettingsFormat == null)
                return;

            // no need to migrate those computed at frame value
            //newFrameSettingsFormat.diffuseGlobalDimmer = oldFrameSettingsFormat.diffuseGlobalDimmer;
            //newFrameSettingsFormat.specularGlobalDimmer = oldFrameSettingsFormat.specularGlobalDimmer;

            // Data
            switch (oldFrameSettingsFormat.shaderLitMode)
            {
                case ObsoleteLitShaderMode.Forward:
                    newFrameSettingsFormat.litShaderMode = LitShaderMode.Forward;
                    break;
                case ObsoleteLitShaderMode.Deferred:
                    newFrameSettingsFormat.litShaderMode = LitShaderMode.Deferred;
                    break;
                default:
                    throw new ArgumentException("Unknown ObsoleteLitShaderMode");
            }

            newFrameSettingsFormat.SetEnabled(FrameSettingsField.ShadowMaps, oldFrameSettingsFormat.enableShadow);
            newFrameSettingsFormat.SetEnabled(FrameSettingsField.ContactShadows, oldFrameSettingsFormat.enableContactShadows);
            newFrameSettingsFormat.SetEnabled(FrameSettingsField.Shadowmask, oldFrameSettingsFormat.enableShadowMask);
            newFrameSettingsFormat.SetEnabled(FrameSettingsField.SSR, oldFrameSettingsFormat.enableSSR);
            newFrameSettingsFormat.SetEnabled(FrameSettingsField.SSAO, oldFrameSettingsFormat.enableSSAO);
            newFrameSettingsFormat.SetEnabled(FrameSettingsField.SubsurfaceScattering, oldFrameSettingsFormat.enableSubsurfaceScattering);
            newFrameSettingsFormat.SetEnabled(FrameSettingsField.Transmission, oldFrameSettingsFormat.enableTransmission);
            newFrameSettingsFormat.SetEnabled(FrameSettingsField.AtmosphericScattering, oldFrameSettingsFormat.enableAtmosphericScattering);
            newFrameSettingsFormat.SetEnabled(FrameSettingsField.Volumetrics, oldFrameSettingsFormat.enableVolumetrics);
            newFrameSettingsFormat.SetEnabled(FrameSettingsField.ReprojectionForVolumetrics, oldFrameSettingsFormat.enableReprojectionForVolumetrics);
            newFrameSettingsFormat.SetEnabled(FrameSettingsField.LightLayers, oldFrameSettingsFormat.enableLightLayers);
            newFrameSettingsFormat.SetEnabled(FrameSettingsField.DepthPrepassWithDeferredRendering, oldFrameSettingsFormat.enableDepthPrepassWithDeferredRendering);
            newFrameSettingsFormat.SetEnabled(FrameSettingsField.TransparentPrepass, oldFrameSettingsFormat.enableTransparentPrepass);
            newFrameSettingsFormat.SetEnabled(FrameSettingsField.MotionVectors, oldFrameSettingsFormat.enableMotionVectors);
            newFrameSettingsFormat.SetEnabled(FrameSettingsField.ObjectMotionVectors, oldFrameSettingsFormat.enableObjectMotionVectors);
            newFrameSettingsFormat.SetEnabled(FrameSettingsField.Decals, oldFrameSettingsFormat.enableDecals);
            newFrameSettingsFormat.SetEnabled(FrameSettingsField.Refraction, oldFrameSettingsFormat.enableRoughRefraction);
            newFrameSettingsFormat.SetEnabled(FrameSettingsField.TransparentPostpass, oldFrameSettingsFormat.enableTransparentPostpass);
            newFrameSettingsFormat.SetEnabled(FrameSettingsField.Distortion, oldFrameSettingsFormat.enableDistortion);
            newFrameSettingsFormat.SetEnabled(FrameSettingsField.Postprocess, oldFrameSettingsFormat.enablePostprocess);
            newFrameSettingsFormat.SetEnabled(FrameSettingsField.OpaqueObjects, oldFrameSettingsFormat.enableOpaqueObjects);
            newFrameSettingsFormat.SetEnabled(FrameSettingsField.TransparentObjects, oldFrameSettingsFormat.enableTransparentObjects);
            newFrameSettingsFormat.SetEnabled(FrameSettingsField.MSAA, oldFrameSettingsFormat.enableMSAA);
            newFrameSettingsFormat.SetEnabled(FrameSettingsField.ExposureControl, oldFrameSettingsFormat.enableExposureControl);
            newFrameSettingsFormat.SetEnabled(FrameSettingsField.AsyncCompute, oldFrameSettingsFormat.enableAsyncCompute);
            newFrameSettingsFormat.SetEnabled(FrameSettingsField.LightListAsync, oldFrameSettingsFormat.runLightListAsync);
            newFrameSettingsFormat.SetEnabled(FrameSettingsField.SSRAsync, oldFrameSettingsFormat.runSSRAsync);
            newFrameSettingsFormat.SetEnabled(FrameSettingsField.SSAOAsync, oldFrameSettingsFormat.runSSAOAsync);
            newFrameSettingsFormat.SetEnabled(FrameSettingsField.ContactShadowsAsync, oldFrameSettingsFormat.runContactShadowsAsync);
            newFrameSettingsFormat.SetEnabled(FrameSettingsField.VolumeVoxelizationsAsync, oldFrameSettingsFormat.runVolumeVoxelizationAsync);

            if (oldFrameSettingsFormat.lightLoopSettings != null)
            {
                newFrameSettingsFormat.SetEnabled(FrameSettingsField.DeferredTile, oldFrameSettingsFormat.lightLoopSettings.enableDeferredTileAndCluster);
                newFrameSettingsFormat.SetEnabled(FrameSettingsField.ComputeLightEvaluation, oldFrameSettingsFormat.lightLoopSettings.enableComputeLightEvaluation);
                newFrameSettingsFormat.SetEnabled(FrameSettingsField.ComputeLightVariants, oldFrameSettingsFormat.lightLoopSettings.enableComputeLightVariants);
                newFrameSettingsFormat.SetEnabled(FrameSettingsField.ComputeMaterialVariants, oldFrameSettingsFormat.lightLoopSettings.enableComputeMaterialVariants);
                newFrameSettingsFormat.SetEnabled(FrameSettingsField.FPTLForForwardOpaque, oldFrameSettingsFormat.lightLoopSettings.enableFptlForForwardOpaque);
                newFrameSettingsFormat.SetEnabled(FrameSettingsField.BigTilePrepass, oldFrameSettingsFormat.lightLoopSettings.enableBigTilePrepass);
            }

            // OverrideMask
            newFrameSettingsOverrideMask.mask = new BitArray128();
            Array values = Enum.GetValues(typeof(ObsoleteFrameSettingsOverrides));
            foreach (ObsoleteFrameSettingsOverrides val in values)
            {
                if ((val & oldFrameSettingsFormat.overrides) > 0)
                {
                    switch(val)
                    {
                        case ObsoleteFrameSettingsOverrides.Shadow:
                            newFrameSettingsOverrideMask.mask[(int)FrameSettingsField.ShadowMaps] = true;
                            break;
                        case ObsoleteFrameSettingsOverrides.ContactShadow:
                            newFrameSettingsOverrideMask.mask[(int)FrameSettingsField.ContactShadows] = true;
                            break;
                        case ObsoleteFrameSettingsOverrides.ShadowMask:
                            newFrameSettingsOverrideMask.mask[(int)FrameSettingsField.Shadowmask] = true;
                            break;
                        case ObsoleteFrameSettingsOverrides.SSR:
                            newFrameSettingsOverrideMask.mask[(int)FrameSettingsField.SSR] = true;
                            break;
                        case ObsoleteFrameSettingsOverrides.SSAO:
                            newFrameSettingsOverrideMask.mask[(int)FrameSettingsField.SSAO] = true;
                            break;
                        case ObsoleteFrameSettingsOverrides.SubsurfaceScattering:
                            newFrameSettingsOverrideMask.mask[(int)FrameSettingsField.SubsurfaceScattering] = true;
                            break;
                        case ObsoleteFrameSettingsOverrides.Transmission:
                            newFrameSettingsOverrideMask.mask[(int)FrameSettingsField.Transmission] = true;
                            break;
                        case ObsoleteFrameSettingsOverrides.AtmosphericScaterring:
                            newFrameSettingsOverrideMask.mask[(int)FrameSettingsField.AtmosphericScattering] = true;
                            break;
                        case ObsoleteFrameSettingsOverrides.Volumetrics:
                            newFrameSettingsOverrideMask.mask[(int)FrameSettingsField.Volumetrics] = true;
                            break;
                        case ObsoleteFrameSettingsOverrides.ReprojectionForVolumetrics:
                            newFrameSettingsOverrideMask.mask[(int)FrameSettingsField.ReprojectionForVolumetrics] = true;
                            break;
                        case ObsoleteFrameSettingsOverrides.LightLayers:
                            newFrameSettingsOverrideMask.mask[(int)FrameSettingsField.LightLayers] = true;
                            break;
                        case ObsoleteFrameSettingsOverrides.ShaderLitMode:
                            newFrameSettingsOverrideMask.mask[(int)FrameSettingsField.LitShaderMode] = true;
                            break;
                        case ObsoleteFrameSettingsOverrides.DepthPrepassWithDeferredRendering:
                            newFrameSettingsOverrideMask.mask[(int)FrameSettingsField.DepthPrepassWithDeferredRendering] = true;
                            break;
                        case ObsoleteFrameSettingsOverrides.TransparentPrepass:
                            newFrameSettingsOverrideMask.mask[(int)FrameSettingsField.TransparentPrepass] = true;
                            break;
                        case ObsoleteFrameSettingsOverrides.MotionVectors:
                            newFrameSettingsOverrideMask.mask[(int)FrameSettingsField.MotionVectors] = true;
                            break;
                        case ObsoleteFrameSettingsOverrides.ObjectMotionVectors:
                            newFrameSettingsOverrideMask.mask[(int)FrameSettingsField.ObjectMotionVectors] = true;
                            break;
                        case ObsoleteFrameSettingsOverrides.Decals:
                            newFrameSettingsOverrideMask.mask[(int)FrameSettingsField.Decals] = true;
                            break;
                        case ObsoleteFrameSettingsOverrides.RoughRefraction:
                            newFrameSettingsOverrideMask.mask[(int)FrameSettingsField.Refraction] = true;
                            break;
                        case ObsoleteFrameSettingsOverrides.TransparentPostpass:
                            newFrameSettingsOverrideMask.mask[(int)FrameSettingsField.TransparentPostpass] = true;
                            break;
                        case ObsoleteFrameSettingsOverrides.Distortion:
                            newFrameSettingsOverrideMask.mask[(int)FrameSettingsField.Distortion] = true;
                            break;
                        case ObsoleteFrameSettingsOverrides.Postprocess:
                            newFrameSettingsOverrideMask.mask[(int)FrameSettingsField.Postprocess] = true;
                            break;
                        case ObsoleteFrameSettingsOverrides.OpaqueObjects:
                            newFrameSettingsOverrideMask.mask[(int)FrameSettingsField.OpaqueObjects] = true;
                            break;
                        case ObsoleteFrameSettingsOverrides.TransparentObjects:
                            newFrameSettingsOverrideMask.mask[(int)FrameSettingsField.TransparentObjects] = true;
                            break;
                        case ObsoleteFrameSettingsOverrides.MSAA:
                            newFrameSettingsOverrideMask.mask[(int)FrameSettingsField.MSAA] = true;
                            break;
                        case ObsoleteFrameSettingsOverrides.ExposureControl:
                            newFrameSettingsOverrideMask.mask[(int)FrameSettingsField.ExposureControl] = true;
                            break;
                        case ObsoleteFrameSettingsOverrides.AsyncCompute:
                            newFrameSettingsOverrideMask.mask[(int)FrameSettingsField.AsyncCompute] = true;
                            break;
                        case ObsoleteFrameSettingsOverrides.LightListAsync:
                            newFrameSettingsOverrideMask.mask[(int)FrameSettingsField.LightListAsync] = true;
                            break;
                        case ObsoleteFrameSettingsOverrides.SSRAsync:
                            newFrameSettingsOverrideMask.mask[(int)FrameSettingsField.SSRAsync] = true;
                            break;
                        case ObsoleteFrameSettingsOverrides.SSAOAsync:
                            newFrameSettingsOverrideMask.mask[(int)FrameSettingsField.SSAOAsync] = true;
                            break;
                        case ObsoleteFrameSettingsOverrides.ContactShadowsAsync:
                            newFrameSettingsOverrideMask.mask[(int)FrameSettingsField.ContactShadowsAsync] = true;
                            break;
                        case ObsoleteFrameSettingsOverrides.VolumeVoxelizationsAsync:
                            newFrameSettingsOverrideMask.mask[(int)FrameSettingsField.VolumeVoxelizationsAsync] = true;
                            break;
                        default:
                            throw new ArgumentException("Unknown ObsoleteFrameSettingsOverride, was " + val);
                    }
                }
            }

            if (oldFrameSettingsFormat.lightLoopSettings != null)
            {
                values = Enum.GetValues(typeof(ObsoleteLightLoopSettingsOverrides));
                foreach (ObsoleteLightLoopSettingsOverrides val in values)
                {
                    if ((val & oldFrameSettingsFormat.lightLoopSettings.overrides) > 0)
                    {
                        switch (val)
                        {
                            case ObsoleteLightLoopSettingsOverrides.TileAndCluster:
                                newFrameSettingsOverrideMask.mask[(int)FrameSettingsField.DeferredTile] = true;
                                break;
                            case ObsoleteLightLoopSettingsOverrides.BigTilePrepass:
                                newFrameSettingsOverrideMask.mask[(int)FrameSettingsField.BigTilePrepass] = true;
                                break;
                            case ObsoleteLightLoopSettingsOverrides.ComputeLightEvaluation:
                                newFrameSettingsOverrideMask.mask[(int)FrameSettingsField.ComputeLightEvaluation] = true;
                                break;
                            case ObsoleteLightLoopSettingsOverrides.ComputeLightVariants:
                                newFrameSettingsOverrideMask.mask[(int)FrameSettingsField.ComputeLightVariants] = true;
                                break;
                            case ObsoleteLightLoopSettingsOverrides.ComputeMaterialVariants:
                                newFrameSettingsOverrideMask.mask[(int)FrameSettingsField.ComputeMaterialVariants] = true;
                                break;
                            case ObsoleteLightLoopSettingsOverrides.FptlForForwardOpaque:
                                newFrameSettingsOverrideMask.mask[(int)FrameSettingsField.FPTLForForwardOpaque] = true;
                                break;
                            default:
                                throw new ArgumentException("Unknown ObsoleteLightLoopSettingsOverrides");
                        }
                    }
                }
            }

            //free space:
            oldFrameSettingsFormat = null;
        }
#pragma warning restore 618 // Type or member is obsolete

        internal static void MigrateToCustomPostprocessAndCustomPass(ref FrameSettings cameraFrameSettings)
        {
            cameraFrameSettings.SetEnabled(FrameSettingsField.CustomPass, true);
            cameraFrameSettings.SetEnabled(FrameSettingsField.CustomPostProcess, true);
        }

        internal static void MigrateToAfterPostprocess(ref FrameSettings cameraFrameSettings)
        {
            cameraFrameSettings.SetEnabled(FrameSettingsField.AfterPostprocess, true);
        }

        internal static void MigrateToDefaultReflectionSettings(ref FrameSettings cameraFrameSettings)
        {
            cameraFrameSettings.SetEnabled(FrameSettingsField.ReflectionProbe, true);
            cameraFrameSettings.SetEnabled(FrameSettingsField.PlanarProbe, true);
            cameraFrameSettings.SetEnabled(FrameSettingsField.ReplaceDiffuseForIndirect, false);
            cameraFrameSettings.SetEnabled(FrameSettingsField.SkyReflection, true);
        }

        internal static void MigrateToNoReflectionRealtimeSettings(ref FrameSettings cameraFrameSettings)
        {
            cameraFrameSettings.SetEnabled(FrameSettingsField.ReflectionProbe, true);
            cameraFrameSettings.SetEnabled(FrameSettingsField.PlanarProbe, false);
            cameraFrameSettings.SetEnabled(FrameSettingsField.ReplaceDiffuseForIndirect, false);
            cameraFrameSettings.SetEnabled(FrameSettingsField.SkyReflection, true);
        }

        internal static void MigrateToNoReflectionSettings(ref FrameSettings cameraFrameSettings)
        {
            cameraFrameSettings.SetEnabled(FrameSettingsField.ReflectionProbe, false);
            cameraFrameSettings.SetEnabled(FrameSettingsField.PlanarProbe, false);
            cameraFrameSettings.SetEnabled(FrameSettingsField.ReplaceDiffuseForIndirect, true);
            cameraFrameSettings.SetEnabled(FrameSettingsField.SkyReflection, false);
        }

        internal static void MigrateToPostProcess(ref FrameSettings cameraFrameSettings)
        {
            cameraFrameSettings.SetEnabled(FrameSettingsField.StopNaN, true);
            cameraFrameSettings.SetEnabled(FrameSettingsField.DepthOfField, true);
            cameraFrameSettings.SetEnabled(FrameSettingsField.MotionBlur, true);
            cameraFrameSettings.SetEnabled(FrameSettingsField.PaniniProjection, true);
            cameraFrameSettings.SetEnabled(FrameSettingsField.Bloom, true);
            cameraFrameSettings.SetEnabled(FrameSettingsField.LensDistortion, true);
            cameraFrameSettings.SetEnabled(FrameSettingsField.ChromaticAberration, true);
            cameraFrameSettings.SetEnabled(FrameSettingsField.Vignette, true);
            cameraFrameSettings.SetEnabled(FrameSettingsField.ColorGrading, true);
            cameraFrameSettings.SetEnabled(FrameSettingsField.FilmGrain, true);
            cameraFrameSettings.SetEnabled(FrameSettingsField.Dithering, true);
            cameraFrameSettings.SetEnabled(FrameSettingsField.Antialiasing, true);
        }

        internal static void MigrateToDirectSpecularLighting(ref FrameSettings cameraFrameSettings)
        {
            cameraFrameSettings.SetEnabled(FrameSettingsField.DirectSpecularLighting, true);
        }

        internal static void MigrateToNoDirectSpecularLighting(ref FrameSettings cameraFrameSettings)
        {
            cameraFrameSettings.SetEnabled(FrameSettingsField.DirectSpecularLighting, false);
        }

        internal static void MigrateToRayTracing(ref FrameSettings cameraFrameSettings)
        {
            cameraFrameSettings.SetEnabled(FrameSettingsField.RayTracing, true);
        }

        internal static void MigrateToSeparateColorGradingAndTonemapping(ref FrameSettings cameraFrameSettings)
        {
            cameraFrameSettings.SetEnabled(FrameSettingsField.Tonemapping, true);
        }
    }
}
