


using System;
using UnityEditor;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;

static class URPTargetUtils
{
    internal static Target GetUniversalTarget() // Temp code.
    {
        var targetTypes = TypeCache.GetTypesDerivedFrom<Target>();
        foreach (var type in targetTypes)
        {
            if (type.IsAbstract || type.IsGenericType || !type.IsClass || type.Name != "UniversalTarget")
                continue;

            var target = (Target)Activator.CreateInstance(type);
            if (!target.isHidden)
                return target;
        }
        return null;
    }


    internal static Target ConfigureURPLit(GraphHandler graph)
    {
        var target = GetUniversalTarget();

        // Mapped to these customization points:
        ShaderGraphAssetUtils.RebuildContextNodes(graph, target);


        var fragOut = graph.GetNode(ShaderGraphAssetUtils.kMainEntryContextName);
        // TODO: Set VertOut fields to be the appropriate referables, though they show up- modifying them doesn't get picked up atm.

        GraphTypeHelpers.SetAsVec3(fragOut.GetPort("BaseColor").GetTypeField(), new Vector3(.5f, .5f, .5f));
        GraphTypeHelpers.SetAsVec3(fragOut.GetPort("NormalTS").GetTypeField(), Vector3.forward);
        GraphTypeHelpers.SetAsFloat(fragOut.GetPort("Occlusion").GetTypeField(), 1f);
        GraphTypeHelpers.SetAsFloat(fragOut.GetPort("Smoothness").GetTypeField(), 0.5f);
        GraphTypeHelpers.SetAsFloat(fragOut.GetPort("Metallic").GetTypeField(), 0f);

        return target;
    }


}
