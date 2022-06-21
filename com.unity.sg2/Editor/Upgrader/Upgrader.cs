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
            foreach (var node in graphData.GetNodes<AbstractMaterialNode>())
            {
                ConvertAndAdd(node);
                //RegistryKey registryKey = AbstractMaterialNodeToRegistryKey(node);
                //if (registryKey == null)
                //{
                //    // couldn't find a matching key in the registry
                //    continue;
                //}
                //// add a node to the new graph
                //graphHandler.AddNode(registryKey);
            }

            // add the result to the SG2

            // save
            ShaderGraphAssetUtils.HandleSave(newAssetPath, asset);
        }

        /// <summary>
        /// Returns an instance of registered node in SG2 that best matches the provided SG1 node.
        /// </summary>
        internal static void ConvertNode(AbstractMaterialNode sg1Node)
        {
            throw new Exception("UNIMPLEMENTED");
        }

        internal static void ConvertAndAdd(AbstractMaterialNode node)
        {

        }

        private static RegistryKey AbstractMaterialNodeToRegistryKey(AbstractMaterialNode node)
        {
            throw new Exception("UNIMPLEMENTED");
        }
    }
}
