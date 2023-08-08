#if UNITY_EDITOR
using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.Rendering;

namespace Common
{
    public static class RenderPipelineUtils
    {
        public static RenderPipelineAsset LoadAsset(Type renderPipelineType)
        {
            var assets = AssetDatabase.FindAssets($"t: {renderPipelineType.Name}");
            Assert.IsNotEmpty(assets, $"There is no {renderPipelineType.Name} in the project. It required to run SRP Graphics Settings tests.");

            var path = AssetDatabase.GUIDToAssetPath(assets[0]);
            var asset = AssetDatabase.LoadAssetAtPath(path, renderPipelineType) as RenderPipelineAsset;
            Assert.IsNotNull(asset, $"{renderPipelineType.Name} is not inherit from {nameof(RenderPipelineAsset)}");
            return asset;
        }
    }
}
#endif
