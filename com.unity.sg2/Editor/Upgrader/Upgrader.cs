using System;
using UnityEngine;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Utils
{
    internal class ShaderGraphUpgrader
    {
        /// <summary>
        /// Upgrades a v1 GraphData object to a v2 shader graph
        /// </summary>
        /// <param name="graphData"></param>
        internal static void Upgrade(string newAssetPath, GraphData graphData)
        {
            Debug.LogError($"GraphData : {graphData}");
            // create a SG2 ShaderGraphAsset
            var asset = ShaderGraphAssetUtils.CreateNewAssetGraph(isSubGraph: false);

            // get the Registry and GraphHandler from the ShaderGraphModel
            var graphHandler = asset.ShaderGraphModel.GraphHandler;
            var registry = asset.ShaderGraphModel.RegistryInstance;

            // get the node data from the SG1 graph
            var nodeConverter = new NodeConverter(graphHandler);
            var nodes = graphData.GetNodes<AbstractMaterialNode>();
            foreach (var node in nodes)
            {
                nodeConverter.Convert(node);
            }

            // add the result to the SG2

            // save
            ShaderGraphAssetUtils.HandleSave(newAssetPath, asset);
        }
    }
}
