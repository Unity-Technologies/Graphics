using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine.Rendering.VirtualTexturing;

namespace UnityEngine.Rendering.HighDefinition
{
#if ENABLE_VIRTUALTEXTURES
    [HelpURL(Documentation.baseURL + Documentation.version + Documentation.subURL + "VirtualTexturing - Settings" + Documentation.endURL)]
    [Serializable, VolumeComponentMenu("VirtualTexturing")]
    public sealed class VirtualTexturingSettings : VolumeComponent
    {
        public NoInterpMinIntParameter cpuCacheSizeInMegaBytes = new NoInterpMinIntParameter(256, 2);
        public NoInterpMinIntParameter gpuCacheSizeInMegaBytes = new NoInterpMinIntParameter(64, 2);

        // Explicit override parameter here because ObjectParameter<T> has an overridestate that is always true.
        public bool gpuCacheSizeOverridesOverridden = false;
        // On the UI side we split the GPU cache size overrides into different lists based on the usage value.
        public List<VirtualTexturingGPUCacheSizeOverride> gpuCacheSizeOverridesShared = new List<VirtualTexturingGPUCacheSizeOverride>();
        public List<VirtualTexturingGPUCacheSizeOverride> gpuCacheSizeOverridesStreaming = new List<VirtualTexturingGPUCacheSizeOverride>();
        public List<VirtualTexturingGPUCacheSizeOverride> gpuCacheSizeOverridesProcedural = new List<VirtualTexturingGPUCacheSizeOverride>(); 

        // Returns ettings as passed to the Virtual Texturing API, pass the result of this function to another call of GetSettings() to collapse multiple overrides together.
        public VirtualTexturing.VirtualTexturingSettings GetSettings(VirtualTexturing.VirtualTexturingSettings settings)
        {
            if (cpuCacheSizeInMegaBytes.overrideState)
            {
                settings.cpuCache.sizeInMegaBytes = (uint) cpuCacheSizeInMegaBytes.value;
            }
            else
            {
                settings.cpuCache.sizeInMegaBytes = 256;
            }

          
            if (gpuCacheSizeOverridesOverridden)
            {
                List<VirtualTexturingGPUCacheSizeOverride> overrides = new List<VirtualTexturingGPUCacheSizeOverride>();
                overrides.AddRange(gpuCacheSizeOverridesShared);
                overrides.AddRange(gpuCacheSizeOverridesStreaming);
                overrides.AddRange(gpuCacheSizeOverridesProcedural);
                settings.gpuCache.sizeOverrides = overrides.ToArray();
            }

            if (gpuCacheSizeInMegaBytes.overrideState)
            {
                settings.gpuCache.sizeInMegaBytes = (uint) gpuCacheSizeInMegaBytes.value;
            }
            else
            {
                settings.gpuCache.sizeInMegaBytes = 64;
            }

            return settings;
        }

        void OnValidate()
        {
            var pipelineAsset = GraphicsSettings.currentRenderPipeline as HDRenderPipelineAsset;
            if (pipelineAsset != null && pipelineAsset.defaultVolumeProfile == this)
            {
                UnityEngine.Rendering.VirtualTexturing.System.ApplyVirtualTexturingSettings(GetSettings(new VirtualTexturing.VirtualTexturingSettings()));
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
