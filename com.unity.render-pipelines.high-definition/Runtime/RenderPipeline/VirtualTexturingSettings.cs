using System;
using System.IO;
using UnityEditor;

namespace UnityEngine.Rendering.HighDefinition
{
    [HelpURL(Documentation.baseURL + Documentation.version + Documentation.subURL + "VirtualTexturing - Settings" + Documentation.endURL)]
    public sealed class VirtualTexturingSettings : ScriptableObject
    {
        public VirtualTexturing.VirtualTexturingSettings settings;

        public VirtualTexturingSettings()
        {
            settings = new VirtualTexturing.VirtualTexturingSettings();
            settings.cpuCache.sizeInMegaBytes = 256;
            settings.gpuCache.sizeInMegaBytes = 64;
        }
    }
}
