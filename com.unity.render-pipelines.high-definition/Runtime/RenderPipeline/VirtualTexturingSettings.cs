using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine.Rendering.VirtualTexturing;

namespace UnityEngine.Rendering.HighDefinition
{
    [HelpURL(Documentation.baseURL + Documentation.version + Documentation.subURL + "VirtualTexturing - Settings" + Documentation.endURL)]
    [Serializable, VolumeComponentMenu("VirtualTexturing")]
    public sealed class VirtualTexturingSettings : VolumeComponent
    {
#if ENABLE_VIRTUALTEXTURES

        public NoInterpMinIntParameter cpuCacheSizeInMegaBytes = new NoInterpMinIntParameter(256, 2);
        public NoInterpMinIntParameter gpuCacheSizeInMegaBytes = new NoInterpMinIntParameter(64, 2);

        // Explicit override parameter here because ObjectParameter<T> has an overridestate that is always true.
        public bool gpuCacheSizeOverridesOverridden = false;
        // On the UI side we split the GPU cache size overrides into different lists based on the usage value.
        public List<VirtualTexturingGPUCacheSizeOverride> gpuCacheSizeOverridesShared = new List<VirtualTexturingGPUCacheSizeOverride>();
        public List<VirtualTexturingGPUCacheSizeOverride> gpuCacheSizeOverridesStreaming = new List<VirtualTexturingGPUCacheSizeOverride>();
        public List<VirtualTexturingGPUCacheSizeOverride> gpuCacheSizeOverridesProcedural = new List<VirtualTexturingGPUCacheSizeOverride>(); 

        // Settings as passed to the Virtual Texturing API
        public VirtualTexturing.VirtualTexturingSettings Settings
        {
            get
            {
                VirtualTexturing.VirtualTexturingSettings settings = new VirtualTexturing.VirtualTexturingSettings();

                settings.cpuCache = new VirtualTexturingCPUCacheSettings();
                settings.cpuCache.sizeInMegaBytes = (uint)cpuCacheSizeInMegaBytes.value;

                List<VirtualTexturingGPUCacheSizeOverride> overrides = new List<VirtualTexturingGPUCacheSizeOverride>();
                overrides.AddRange(gpuCacheSizeOverridesShared);
                overrides.AddRange(gpuCacheSizeOverridesStreaming);
                overrides.AddRange(gpuCacheSizeOverridesProcedural);

                settings.gpuCache.sizeOverrides = overrides.ToArray();
                settings.gpuCache.sizeInMegaBytes = (uint)gpuCacheSizeInMegaBytes.value;

                return settings;
            }
        }

        void OnValidate()
        {
            var pipelineAsset = GraphicsSettings.currentRenderPipeline as HDRenderPipelineAsset;
            if (pipelineAsset != null && pipelineAsset.defaultVolumeProfile == this)
            {
                UnityEngine.Rendering.VirtualTexturing.System.ApplyVirtualTexturingSettings(Settings);
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
