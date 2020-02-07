using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine.Rendering.VirtualTexturing;

namespace UnityEngine.Rendering.HighDefinition
{
    // On the UI side we split the GPU cache size overrides into different lists based on the usage value.
    // To have the serialized side be a 1:1 match with what's shown in the UI we have an HDRP side version of the GPU cache size settings.
    [Serializable]
    public struct VirtualTexturingGPUCacheSettings
    {
        public uint sizeInMegaBytes;
        public VirtualTexturingGPUCacheSizeOverride[] gpuCacheSizeOverridesShared;
        public VirtualTexturingGPUCacheSizeOverride[] gpuCacheSizeOverridesStreaming;
        public VirtualTexturingGPUCacheSizeOverride[] gpuCacheSizeOverridesProcedural;
    }

    [HelpURL(Documentation.baseURL + Documentation.version + Documentation.subURL + "VirtualTexturing - Settings" + Documentation.endURL)]
    public sealed class VirtualTexturingSettings : ScriptableObject
    {
#if ENABLE_VIRTUALTEXTURES
        public VirtualTexturingCPUCacheSettings cpuCache;
        public VirtualTexturingGPUCacheSettings gpuCache;

        // Get settings as passed to the Virtual Texturing API
        public VirtualTexturing.VirtualTexturingSettings GetSettings()
        {
            VirtualTexturing.VirtualTexturingSettings settings = new VirtualTexturing.VirtualTexturingSettings();

            settings.cpuCache = cpuCache;

            List<VirtualTexturingGPUCacheSizeOverride> overrides = new List<VirtualTexturingGPUCacheSizeOverride>();
            overrides.AddRange(gpuCache.gpuCacheSizeOverridesShared);
            overrides.AddRange(gpuCache.gpuCacheSizeOverridesStreaming);
            overrides.AddRange(gpuCache.gpuCacheSizeOverridesProcedural);

            settings.gpuCache.sizeOverrides = overrides.ToArray();
            settings.gpuCache.sizeInMegaBytes = gpuCache.sizeInMegaBytes;

            return settings;
        }

        public VirtualTexturingSettings()
        {
            cpuCache = new VirtualTexturingCPUCacheSettings();
            cpuCache.sizeInMegaBytes = 256;
            gpuCache = new VirtualTexturingGPUCacheSettings();
            gpuCache.sizeInMegaBytes = 64;

            gpuCache.gpuCacheSizeOverridesShared = new VirtualTexturingGPUCacheSizeOverride[] { };
            gpuCache.gpuCacheSizeOverridesStreaming = new VirtualTexturingGPUCacheSizeOverride[] { };
            gpuCache.gpuCacheSizeOverridesProcedural = new VirtualTexturingGPUCacheSizeOverride[] { };
        }

        void OnValidate()
        {
            var pipelineAsset = GraphicsSettings.currentRenderPipeline as HDRenderPipelineAsset;
            if (pipelineAsset != null && pipelineAsset.virtualTexturingSettings == this)
            {
                UnityEngine.Rendering.VirtualTexturing.System.ApplyVirtualTexturingSettings(pipelineAsset.virtualTexturingSettings.GetSettings());
            }
        }

        public static UnityEngine.Rendering.VirtualTexturing.VirtualTexturingSettings Default
        {
            get
            {
                UnityEngine.Rendering.VirtualTexturing.VirtualTexturingSettings settings = new UnityEngine.Rendering.VirtualTexturing.VirtualTexturingSettings();
                settings.cpuCache.sizeInMegaBytes = 64;
                settings.gpuCache.sizeInMegaBytes = 128;
                return settings;
            }
        }
#endif
    }
}
