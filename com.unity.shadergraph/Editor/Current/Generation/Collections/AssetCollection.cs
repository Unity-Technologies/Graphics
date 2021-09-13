using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    // this class is used to track asset dependencies in shadergraphs and subgraphs
    // that is: it tracks files that the shadergraph or subgraph must access to generate the shader
    // this data is used to automatically re-import the shadergraphs or subgraphs when any of the tracked files change
    [GenerationAPI]
    internal class AssetCollection
    {
        [Flags]
        public enum Flags
        {
            SourceDependency = 1 << 0,     // ShaderGraph directly reads the source file in the Assets directory
            ArtifactDependency = 1 << 1,     // ShaderGraph reads the import result artifact (i.e. subgraph import)
            IsSubGraph = 1 << 2,     // This asset is a SubGraph (used when we need to get multiple levels of dependencies)
            IncludeInExportPackage = 1 << 3      // This asset should be pulled into any .unitypackages built via "Export Package.."
        }

        internal Dictionary<GUID, Flags> assets = new Dictionary<GUID, Flags>();

        internal IEnumerable<GUID> assetGUIDs { get { return assets.Keys; } }

        public AssetCollection()
        {
        }

        internal void Clear()
        {
            assets.Clear();
        }

        // these are assets that we read the source files directly (directly reading the file out of the Assets directory)
        public void AddAssetDependency(GUID assetGUID, Flags flags)
        {
            if (assets.TryGetValue(assetGUID, out Flags existingFlags))
            {
                assets[assetGUID] = existingFlags | flags;
            }
            else
            {
                assets.Add(assetGUID, flags);
            }
        }
    }
}
