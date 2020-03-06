using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal class TargetSetupContext
    {
        public TargetImplementationData data { get; private set; }
        public SubShaderDescriptor descriptor { get; private set; }
        public List<string> assetDependencyPaths { get; private set; }

        public TargetSetupContext()
        {
            assetDependencyPaths = new List<string>();
        }

        public void SetData(TargetImplementationData data)
        {
            this.data = data;
        }

        public void SetupSubShader(SubShaderDescriptor descriptor)
        {
            this.descriptor = descriptor;
        }

        public void AddAssetDependencyPath(string path)
        {
            assetDependencyPaths.Add(path);
        }
    }
}
