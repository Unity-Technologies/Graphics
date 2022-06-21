using System;
using System.Linq;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;

public class NodeConverter
{
    Registry registry;
    GraphHandler graphHandler;

    public NodeConverter(GraphHandler graphHandler)
    {
        this.graphHandler = graphHandler;
    }

    internal void Convert(object node, Type t)
    {
        bool Test(System.Reflection.MethodInfo method)
        {
            if (!method.Name.Equals("Convert"))
            {
                Debug.Log(method.Name);
                return false;
            }
            var parameters = method.GetParameters();
            var ty = parameters.First().ParameterType;
            Debug.Log(ty);
            return ty.Equals(t);
        }
        var bindingFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var allMethods = typeof(NodeConverter).GetMethods(bindingFlags);
        //var matchingMethods = allMethods.Where(x => x.Name.Equals("Convert");
        var matchingMethods = allMethods.Where(Test);
        if (!matchingMethods.Any())
            return;
        var match = matchingMethods.First();
        match.Invoke(this, new object[] { System.Convert.ChangeType(node, t) });
    }

    internal void Convert(AddNode node)
    {
        graphHandler.AddNode(new RegistryKey() { Name = "Add", Version = 1 }, "anything");

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
