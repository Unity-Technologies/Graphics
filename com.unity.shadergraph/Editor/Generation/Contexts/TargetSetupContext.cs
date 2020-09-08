using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal class TargetSetupContext
    {
        public List<SubShaderDescriptor> subShaders { get; private set; }
        public HashSet<GUID> assetDependencyGUIDs { get; private set; }
        public string defaultShaderGUI { get; private set; }

        // pass a HashSet to the constructor to have it gather asset dependency GUIDs
        public TargetSetupContext(HashSet<GUID> assetDependencyGUIDs = null)
        {
            subShaders = new List<SubShaderDescriptor>();
            this.assetDependencyGUIDs = assetDependencyGUIDs;
        }

        public void AddSubShader(SubShaderDescriptor subShader)
        {
            subShaders.Add(subShader);
        }

        public void AddAssetDependencyGUID(GUID guid)
        {
            assetDependencyGUIDs?.Add(guid);
        }

        public void SetDefaultShaderGUI(string defaultShaderGUI)
        {
            this.defaultShaderGUI = defaultShaderGUI;
        }
    }
}
