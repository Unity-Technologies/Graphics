using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine.Rendering.VirtualTexturing;

namespace UnityEngine.Rendering.HighDefinition
{
#if ENABLE_VIRTUALTEXTURES
    [HelpURL(Documentation.baseURL + Documentation.version + Documentation.subURL + "VirtualTexturing - Settings" + Documentation.endURL)]
    [Serializable]
    public sealed class VirtualTexturingSettings
    {
        public int cpuCacheSizeInMegaBytes = 256;
        public int gpuCacheSizeInMegaBytes = 64;

        // On the UI side we split the GPU cache size overrides into different lists based on the usage value.
        public List<VirtualTexturingGPUCacheSizeOverride> gpuCacheSizeOverridesShared = new List<VirtualTexturingGPUCacheSizeOverride>();
        public List<VirtualTexturingGPUCacheSizeOverride> gpuCacheSizeOverridesStreaming = new List<VirtualTexturingGPUCacheSizeOverride>();
        public List<VirtualTexturingGPUCacheSizeOverride> gpuCacheSizeOverridesProcedural = new List<VirtualTexturingGPUCacheSizeOverride>();

        // Returns settings as passed to the Virtual Texturing API.
        public VirtualTexturing.VirtualTexturingSettings GetSettings()
        {
            VirtualTexturing.VirtualTexturingSettings settings = new VirtualTexturing.VirtualTexturingSettings();

            settings.cpuCache.sizeInMegaBytes = (uint) cpuCacheSizeInMegaBytes;

            List<VirtualTexturingGPUCacheSizeOverride> overrides = new List<VirtualTexturingGPUCacheSizeOverride>();
            overrides.AddRange(gpuCacheSizeOverridesShared);
            overrides.AddRange(gpuCacheSizeOverridesStreaming);
            overrides.AddRange(gpuCacheSizeOverridesProcedural);
            settings.gpuCache.sizeOverrides = overrides.ToArray();

            settings.gpuCache.sizeInMegaBytes = (uint) gpuCacheSizeInMegaBytes;

            return settings;
        }

        void OnValidate()
        {
            var pipelineAsset = GraphicsSettings.currentRenderPipeline as HDRenderPipelineAsset;
            if (pipelineAsset != null && pipelineAsset.virtualTexturingSettings == this)
            {
                UnityEngine.Rendering.VirtualTexturing.System.ApplyVirtualTexturingSettings(GetSettings());
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
    }
#endif
}
