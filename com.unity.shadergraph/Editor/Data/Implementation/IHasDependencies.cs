using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    interface IHasDependencies
    {
        void GetSourceAssetDependencies(AssetCollection assetCollection);
    }
}
