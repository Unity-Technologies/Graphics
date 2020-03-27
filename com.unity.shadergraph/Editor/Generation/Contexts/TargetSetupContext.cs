using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal class TargetSetupContext
    {
        public IMasterNode masterNode { get; private set; }
        public List<SubShaderDescriptor> subShaders { get; private set; }
        public List<string> assetDependencyPaths { get; private set; }
        public string defaultShaderGUI { get; private set; }

        public TargetSetupContext()
        {
            subShaders = new List<SubShaderDescriptor>();
            assetDependencyPaths = new List<string>();
        }

        public void SetMasterNode(IMasterNode masterNode)
        {
            this.masterNode = masterNode;
        }

        public void AddSubShader(SubShaderDescriptor subShader)
        {
            subShaders.Add(subShader);
        }

        public void AddAssetDependencyPath(string path)
        {
            assetDependencyPaths.Add(path);
        }

        public void SetDefaultShaderGUI(string defaultShaderGUI)
        {
            this.defaultShaderGUI = defaultShaderGUI;
        }
    }
}
