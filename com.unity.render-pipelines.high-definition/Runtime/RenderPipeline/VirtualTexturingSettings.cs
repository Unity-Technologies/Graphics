using System;
using System.IO;
using UnityEditor;

namespace UnityEngine.Rendering.HighDefinition
{
    [HelpURL(Documentation.baseURL + Documentation.version + Documentation.subURL + "VirtualTexturing - Settings" + Documentation.endURL)]
    public sealed class VirtualTexturingSettings : ScriptableObject
    {
        public VirtualTexturing.VirtualTexturingSettings settings = new VirtualTexturing.VirtualTexturingSettings();

        public static VirtualTexturing.VirtualTexturingSettings Default
        {
            get
            {
                VirtualTexturing.VirtualTexturingSettings settings = new VirtualTexturing.VirtualTexturingSettings();
                settings.cpuCache.sizeInMegaBytes = 64;
                settings.gpuCache.sizeInMegaBytes = 128;
                return settings;
            }
        }
    }
}
