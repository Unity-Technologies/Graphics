using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;

public class NodeConverter
{
    Registry registry;
    GraphHandler graphHandler;

    public NodeConverter(GraphHandler graphHandler)
    {

    }

    internal void Convert(AddNode node)
    {
        Debug.LogError($"NodeConverter.Convert -- AddNode");
    }

    internal void Convert(AbstractMaterialNode node)
    {
        Debug.LogError($"NodeConverter.Convert -- Abstract");

        //RegistryKey registryKey = AbstractMaterialNodeToRegistryKey(node);
        //if (registryKey == null)
        //{
        //    // couldn't find a matching key in the registry
        //    continue;
        //}
        //// add a node to the new graph
        //graphHandler.AddNode(registryKey);
    }
}
