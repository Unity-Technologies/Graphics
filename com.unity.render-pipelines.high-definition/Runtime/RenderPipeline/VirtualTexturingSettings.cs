using System;
using System.IO;
using UnityEditor;

namespace UnityEngine.Rendering.HighDefinition
{
    [HelpURL(Documentation.baseURL + Documentation.version + Documentation.subURL + "VirtualTexturing - Settings" + Documentation.endURL)]
    public sealed class VirtualTexturingSettings : ScriptableObject
    {
#if ENABLE_VIRTUALTEXTURES
        public UnityEngine.Rendering.VirtualTexturing.VirtualTexturingSettings settings = new UnityEngine.Rendering.VirtualTexturing.VirtualTexturingSettings();

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
