using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine.Rendering.VirtualTexturing;

namespace UnityEngine.Rendering.HighDefinition
{
    [HelpURL(Documentation.baseURL + Documentation.version + Documentation.subURL + "VirtualTexturing - Settings" + Documentation.endURL)]
    public sealed class VirtualTexturingSettings : ScriptableObject
    {
        // Do not directly feed this to VirtualTexturing.ApplyVirtualTexturingSettings(). Pass the result of GetSettings() instead.
        public VirtualTexturing.VirtualTexturingSettings settings;

        public VirtualTexturingGPUCacheSizeOverride[] gpuCacheSizeOverridesShared;
        public VirtualTexturingGPUCacheSizeOverride[] gpuCacheSizeOverridesStreaming;
        public VirtualTexturingGPUCacheSizeOverride[] gpuCacheSizeOverridesProcedural;

        // Get settings as passed to the Virtual Texturing API
        public VirtualTexturing.VirtualTexturingSettings GetSettings()
        {
            List<VirtualTexturingGPUCacheSizeOverride> overrides = new List<VirtualTexturingGPUCacheSizeOverride>();

            overrides.AddRange(gpuCacheSizeOverridesShared);
            overrides.AddRange(gpuCacheSizeOverridesStreaming);
            overrides.AddRange(gpuCacheSizeOverridesProcedural);

            settings.gpuCache.sizeOverrides = overrides.ToArray();

            return settings;
        }

        public VirtualTexturingSettings()
        {
            settings = new VirtualTexturing.VirtualTexturingSettings();
            settings.cpuCache.sizeInMegaBytes = 256;
            settings.gpuCache.sizeInMegaBytes = 64;

            gpuCacheSizeOverridesShared = new VirtualTexturingGPUCacheSizeOverride[] { };
            gpuCacheSizeOverridesStreaming = new VirtualTexturingGPUCacheSizeOverride[] { };
            gpuCacheSizeOverridesProcedural = new VirtualTexturingGPUCacheSizeOverride[] { };
        }
    }
}
