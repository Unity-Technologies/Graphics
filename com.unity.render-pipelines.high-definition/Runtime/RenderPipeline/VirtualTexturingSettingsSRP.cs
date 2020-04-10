using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.VirtualTexturing;

namespace UnityEngine.Rendering.HighDefinition
{
    [HelpURL(Documentation.baseURL + Documentation.version + Documentation.subURL + "VirtualTexturing - Settings" + Documentation.endURL)]
    [Serializable]
    public sealed class VirtualTexturingSettingsSRP
    {
        public int cpuCacheSizeInMegaBytes = 256;
        public int gpuCacheSizeInMegaBytes = 64;

        // On the UI side we only expose the overrides used for streaming.
        public List<VirtualTexturingGPUCacheSizeOverrideSRP> gpuCacheSizeOverridesStreaming = new List<VirtualTexturingGPUCacheSizeOverrideSRP>();

#if ENABLE_VIRTUALTEXTURES
        // Returns settings as passed to the Virtual Texturing API.
        public VirtualTexturing.VirtualTexturingSettings GetSettings()
        {
            VirtualTexturing.VirtualTexturingSettings settings = new VirtualTexturing.VirtualTexturingSettings();

            settings.cpuCache.sizeInMegaBytes = (uint) cpuCacheSizeInMegaBytes;

            List<VirtualTexturingGPUCacheSizeOverride> overrides = new List<VirtualTexturingGPUCacheSizeOverride>();

            foreach (VirtualTexturingGPUCacheSizeOverrideSRP setting in gpuCacheSizeOverridesStreaming)
            {
                overrides.Add(setting.GetNative());
            }

            settings.gpuCache.sizeOverrides = overrides.ToArray();

            settings.gpuCache.sizeInMegaBytes = (uint) gpuCacheSizeInMegaBytes;

            return settings;
        }

        public static UnityEngine.Rendering.VirtualTexturing.VirtualTexturingSettings Default
        {
            get
            {
                UnityEngine.Rendering.VirtualTexturing.VirtualTexturingSettings settings = new UnityEngine.Rendering.VirtualTexturing.VirtualTexturingSettings();
                settings.cpuCache.sizeInMegaBytes = 64;
                settings.gpuCache.sizeInMegaBytes = 256;
                return settings;
            }
        }
#endif
    }


    // HDRP side version of VirtualTexturingGPUCacheSizeOverride that is always available to be serialized even if VT is disabled.
    [Serializable]
    public struct VirtualTexturingGPUCacheSizeOverrideSRP
    {
        public VirtualTexturingCacheUsageSRP usage;
        public GraphicsFormat format;
        public uint sizeInMegaBytes;

#if ENABLE_VIRTUALTEXTURES
        public VirtualTexturingGPUCacheSizeOverride GetNative()
        {
            return new VirtualTexturingGPUCacheSizeOverride(){format = format, sizeInMegaBytes = sizeInMegaBytes, usage = (VirtualTexturingCacheUsage)(int)usage};
        }
#endif
    }

    [Serializable]
    public enum VirtualTexturingCacheUsageSRP
    {
        Any,
        Streaming,
        Procedural,
    }
}
