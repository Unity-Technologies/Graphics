using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.Rendering.HighDefinition
{
    static class RenderPipelineSettingsUtilities
    {
        public static IEnumerable<string> RemoveDLSSKeywords(IEnumerable<string> keywords)
        {
#if ENABLE_NVIDIA && !ENABLE_NVIDIA_MODULE
            // Case 1358409 workaround:
            // Remove all DLSS keyword when the NVIDIA package is not installed.
            return keywords.Where(keyword => keyword.IndexOf("dlss", System.StringComparison.OrdinalIgnoreCase) == -1);
#else
            return keywords;
#endif
        }
    }
}
