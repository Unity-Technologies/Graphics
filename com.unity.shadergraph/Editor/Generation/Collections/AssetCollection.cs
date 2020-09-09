using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    // this class is used to track asset dependencies in shadergraphs and subgraphs
    // that is: it tracks files that the shadergraph or subgraph must access to generate the shader
    // this data is used to automatically re-import the shadergraphs or subgraphs when any of the tracked files change
    public class AssetCollection
    {
        public HashSet<GUID> assetSourceDependencyGUIDs = new HashSet<GUID>();
        public HashSet<GUID> assetArtifactDependencyGUIDs = new HashSet<GUID>();

        public AssetCollection()
        {
        }

        // these are assets that we read the source files directly (directly reading the file out of the Assets directory)
        public void AddAssetSourceDependency(GUID assetGUID)
        {
            assetSourceDependencyGUIDs.Add(assetGUID);
        }

        // these are asstes that we read the imported result (via LoadAssetAtPath or similar)
        public void AddAssetArtifactDependency(GUID assetGUID)
        {
            assetArtifactDependencyGUIDs.Add(assetGUID);
        }
    }
}
