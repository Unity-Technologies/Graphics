using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    interface IHasDependencies
    {
        void GetSourceAssetDependencies(AssetCollection assetCollection);
    }

    interface IHasAssetDependencies
    {
        // Returns true if the implementing object needed to reload.
        // This allows the owning model to escalate whether the node
        // should be updated or not.
        bool Reload(HashSet<string> changedAssetGuids);
    }
}
