using System;
using System.Collections.Generic;
using UnityEditor;

namespace UnityEditor.ShaderGraph.Internal
{
    public struct TargetSetupContext
    {
        public IMasterNode masterNode { get; private set; }
        public SubShaderDescriptor descriptor { get; private set; }
        public List<string> assetDependencyPaths { get; private set; }

        public void SetMasterNode(IMasterNode masterNode)
        {
            this.masterNode = masterNode;
        }

        public void SetupSubShader(SubShaderDescriptor descriptor)
        {
            this.descriptor = descriptor;
        }

        public void AddAssetDependencyPath(string path)
        {
            if(assetDependencyPaths == null)
                assetDependencyPaths = new List<string>();
            
            assetDependencyPaths.Add(AssetDatabase.GUIDToAssetPath(path));
        }
    }
}
