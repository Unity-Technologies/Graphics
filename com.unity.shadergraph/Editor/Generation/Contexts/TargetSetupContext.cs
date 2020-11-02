using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal class TargetSetupContext
    {
        public List<SubShaderDescriptor> subShaders { get; private set; }
        public AssetCollection assetCollection { get; private set; }
        public string defaultShaderGUI { get; private set; }

        // pass a HashSet to the constructor to have it gather asset dependency GUIDs
        public TargetSetupContext(AssetCollection assetCollection = null)
        {
            subShaders = new List<SubShaderDescriptor>();
            this.assetCollection = assetCollection;
        }

        public void AddSubShader(SubShaderDescriptor subShader)
        {
            subShaders.Add(subShader);
        }

        public void AddAssetDependency(GUID guid, AssetCollection.Flags flags)
        {
            assetCollection?.AddAssetDependency(guid, flags);
        }

        public void SetDefaultShaderGUI(string defaultShaderGUI)
        {
            this.defaultShaderGUI = defaultShaderGUI;
        }
    }
}
