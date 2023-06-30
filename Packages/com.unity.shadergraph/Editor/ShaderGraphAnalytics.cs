using System.Collections.Generic;
using System.Linq;
using UnityEngine.Analytics;
using UnityEngine;
using System;

namespace UnityEditor.ShaderGraph
{
    class ShaderGraphAnalytics
    {
        const int k_MaxEventsPerHour = 1000;
        const int k_MaxNumberOfElements = 1000;
        const string k_VendorKey = "unity.shadergraph";
        const string k_EventName = "uShaderGraphUsage";

        [AnalyticInfo(eventName: k_EventName, vendorKey: k_VendorKey, maxEventsPerHour:k_MaxEventsPerHour, maxNumberOfElements:k_MaxNumberOfElements)]
        public class Analytic : IAnalytic
        {
            public Analytic(string assetGuid, GraphData graph)
            {
                this.assetGuid = assetGuid;
                this.graph = graph;
            }

            [Serializable]
            struct AnalyticsData : IAnalytic.IData
            {
                public string nodes;
                public int node_count;
                public string asset_guid;
                public int subgraph_count;
            }

            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
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

                data = new AnalyticsData()
                {
                    nodes = jsonRepr,
                    node_count = nodes.Count(),
                    asset_guid = assetGuid,
                    subgraph_count = subgraphCount
                };


                error = null;
                return true;
            }


            string assetGuid;
            GraphData graph;
        };

        public static void SendShaderGraphEvent(string assetGuid, GraphData graph)
        {
            //The event shouldn't be able to report if this is disabled but if we know we're not going to report
            //Lets early out and not waste time gathering all the data
            if (!EditorAnalytics.enabled)
                return;

            Analytic analytic = new Analytic(assetGuid, graph);

            EditorAnalytics.SendAnalytic(analytic);
        }

        static string DictionaryToJson(Dictionary<string, int> dict)
        {
            var entries = dict.Select(d => $"\"{d.Key}\":{string.Join(",", d.Value)}");
            return "{" + string.Join(",", entries) + "}";
        }
    }
}
