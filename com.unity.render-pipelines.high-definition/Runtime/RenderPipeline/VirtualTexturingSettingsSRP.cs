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
    public sealed class VirtualTexturingSettingsSRP
    {
        public int streamingCpuCacheSizeInMegaBytes = 256;
        public List<VirtualTexturingGPUCacheSetting> streamingGpuCacheSettings = new List<VirtualTexturingGPUCacheSetting>() { new VirtualTexturingGPUCacheSetting() { format = Experimental.Rendering.GraphicsFormat.None, sizeInMegaBytes = 128 } };
    }
#endif
}
