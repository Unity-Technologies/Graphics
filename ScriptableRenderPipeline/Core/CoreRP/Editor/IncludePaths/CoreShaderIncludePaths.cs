using System.Linq;
using UnityEngine;
using System.IO;

namespace UnityEditor.Experimental.Rendering
{
    static class CoreShaderIncludePaths
    {
        [ShaderIncludePath]
        public static string[] GetPaths()
        {
            var srpMarker = Directory.GetFiles(Application.dataPath, "SRPMARKER", SearchOption.AllDirectories).FirstOrDefault();
            var paths = new string[srpMarker == null ? 1 : 2];
            var index = 0;
            if (srpMarker != null)
            {
                var rootPath = Directory.GetParent(srpMarker).ToString();
                paths[index] = Path.Combine(rootPath, "ScriptableRenderPipeline/Core");
                index++;
            }
            paths[index] = Path.GetFullPath("Packages/com.unity.render-pipelines.core");
            return paths;
        }
    }
}
