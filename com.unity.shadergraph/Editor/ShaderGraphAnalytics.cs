using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
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
        const int k_Version = 1;

        static bool EnableAnalytics()
        {
            AnalyticsResult result = EditorAnalytics.RegisterEventWithLimit(k_EventName, k_MaxEventsPerHour, k_MaxNumberOfElements, k_VendorKey, k_Version);
            if (result == AnalyticsResult.Ok)
                s_EventRegistered = true;

            return s_EventRegistered;
        }

        private struct AnalyticsData
        {
            public string nodes;
            public int node_count;
            public string main_guid;
            public int subgraph_count;
        }

        public static void SendShaderGraphEvent(string mainGuid, IEnumerable<AbstractMaterialNode> nodes)
        {
            //The event shouldn't be able to report if this is disabled but if we know we're not going to report
            //Lets early out and not waste time gathering all the data
            if (!UnityEngine.Analytics.Analytics.enabled)
                return;

            if (!EnableAnalytics())
                return;

            Dictionary<string, int> nodeTypeAndCount = new Dictionary<string, int>();

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

            var jsonRepr = JsonConvert.SerializeObject(nodeTypeAndCount);
            var data = new AnalyticsData()
            {
                nodes = jsonRepr,
                node_count = nodes.Count(),
                main_guid = mainGuid,
                subgraph_count = subgraphCount
            };

            EditorAnalytics.SendEventWithLimit(k_EventName, data);
        }
    }
}
