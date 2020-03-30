using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal class TargetSetupContext
    {
        public List<SubShaderDescriptor> subShaders { get; private set; }
        public List<string> assetDependencyPaths { get; private set; }

        public TargetSetupContext()
        {
            subShaders = new List<SubShaderDescriptor>();
            assetDependencyPaths = new List<string>();
        }

        public void AddSubShader(SubShaderDescriptor descriptor)
        {
            subShaders.Add(descriptor);
        }

        public void AddAssetDependencyPath(string path)
        {
            assetDependencyPaths.Add(path);
        }
    }
}
