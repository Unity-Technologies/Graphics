using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    public static class GraphAssetCreationHelpers<TGraphAssetType>
        where TGraphAssetType : ScriptableObject, IGraphAsset
    {
        internal static TGraphAssetType CreateInMemoryGraphAsset(Type stencilType, string name,
            IGraphTemplate graphTemplate = null)
        {
            return CreateGraphAsset(stencilType, name, null, graphTemplate);
        }

        internal static TGraphAssetType CreateGraphAsset(Type stencilType, string name, string assetPath,
            IGraphTemplate graphTemplate = null)
        {
            return (TGraphAssetType)GraphAssetCreationHelpers.CreateGraphAsset(typeof(TGraphAssetType), stencilType, name, assetPath, graphTemplate);
        }

        internal static TGraphAssetType CreateGraphAsset(string name, string assetPath)
        {
            return CreateGraphAsset(null, name, assetPath);
        }
    }
}
