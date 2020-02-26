using System.Collections.Generic;
using System.Linq;
using UnityEngine.Analytics;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    class ShaderGraphAnalytics
    {
        static bool s_EventRegistered = false;
        const int k_MaxEventsPerHour = 1000;
        const int k_MaxNumberOfElements = 1000;
        const string k_VendorKey = "unity.shadergraph";
        const string k_EventName = "uShaderGraphUsage";

        static bool EnableAnalytics()
        {
            AnalyticsResult result = EditorAnalytics.RegisterEventWithLimit(k_EventName, k_MaxEventsPerHour, k_MaxNumberOfElements, k_VendorKey);
            if (result == AnalyticsResult.Ok)
                s_EventRegistered = true;

            return s_EventRegistered;
        }

        struct AnalyticsData
        {
            public string nodes;
            public int node_count;
            public string asset_guid;
            public int subgraph_count;
        }

        internal static void SendShaderGraphEvent(string assetGuid, GraphData graph)
        {
            //The event shouldn't be able to report if this is disabled but if we know we're not going to report
            //Lets early out and not waste time gathering all the data
            if (!EditorAnalytics.enabled)
                return;

            if (!EnableAnalytics())
                return;

            Dictionary<string, int> nodeTypeAndCount = new Dictionary<string, int>();
            var nodes = graph.GetNodes<AbstractMaterialNode>();

            int subgraphCount = 0;
            foreach (var materialNode in nodes)
            {
                string nType = materialNode.GetType().ToString().Split('.').Last();

                if (nType == "SubGraphNode")
                {
                    subgraphCount += 1;
                }

                if (!nodeTypeAndCount.ContainsKey(nType))
                {
                    nodeTypeAndCount[nType] = 1;
                }
                else
                {
                    nodeTypeAndCount[nType] += 1;
                }
            }
            var jsonRepr = DictionaryToJson(nodeTypeAndCount);

            var data = new AnalyticsData()
            {
                nodes = jsonRepr,
                node_count = nodes.Count(),
                asset_guid = assetGuid,
                subgraph_count = subgraphCount
            };

            EditorAnalytics.SendEventWithLimit(k_EventName, data);
        }

        static string DictionaryToJson(Dictionary<string, int> dict)
        {
            var entries = dict.Select(d => $"\"{d.Key}\":{string.Join(",", d.Value)}");
            return "{" + string.Join(",", entries) + "}";
        }
    }
}
