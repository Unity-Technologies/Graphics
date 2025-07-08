using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine.Analytics;

namespace UnityEditor.ShaderGraph
{
    class ShaderGraphAnalytics
    {
        const int k_MaxEventsPerHour = 1000;
        const int k_MaxNumberOfElements = 1000;
        const string k_VendorKey = "unity.shadergraph";
        const string k_EventName = "uShaderGraphUsage";
        const string k_TemplateEventName = "uShaderGraphCreateFromTemplate";

        [AnalyticInfo(eventName: k_TemplateEventName, vendorKey: k_VendorKey)]
        internal class ShaderGraphTemplateAnalytic : IAnalytic
        {
            [Serializable]
            struct AnalyticsData : IAnalytic.IData
            {
                public string template_name;
                public string template_path;
                public string template_category;
                public string template_guid;
                
                public string hdrp_material;
                public string urp_material;
                public string builtin_material;
                public string rt_material;

                public bool hdrp_vfx;
                public bool urp_vfx;
                public bool vfx_legacy;
                public List<string> additional_terms;
            }

            string template_name;
            string template_path;
            string template_category;
            string template_guid;

            string hdrp_material = string.Empty;
            string urp_material = string.Empty;
            string builtin_material = string.Empty;
            string rt_material = string.Empty;

            bool hdrp_vfx = false;
            bool urp_vfx = false;
            bool vfx_legacy = false;

            HashSet<string> additional_terms = new();

            internal ShaderGraphTemplateAnalytic(GraphViewTemplateDescriptor descriptor)
            {
                var path = AssetDatabase.GUIDToAssetPath(descriptor.assetGuid);
                bool fromUnity = path?.Contains("com.unity") ?? false;

                template_name = fromUnity ? descriptor.name : "hidden";
                template_path = fromUnity ? path : "hidden";
                template_category = fromUnity ? descriptor.category : "hidden";
                template_guid = descriptor.assetGuid;

                if (FileUtilities.TryReadGraphDataFromDisk(path, out GraphData graph))
                {
                    foreach (var target in graph.activeTargets)
                    {
                        switch (target.GetType().Name)
                        {
                            case "HDTarget":
                                hdrp_material = target.activeSubTarget?.displayName;
                                hdrp_vfx = target.SupportsVFX();
                                break;
                            case "UniversalTarget":
                                urp_material = target.activeSubTarget?.displayName;
                                urp_vfx = target.SupportsVFX();
                                break;

                            case "BuiltInTarget": builtin_material = target.activeSubTarget?.displayName; break;
                            case "CustomRenderTextureTarget": rt_material = target.activeSubTarget?.displayName; break;
                            case "VFXTarget": vfx_legacy = target.SupportsVFX(); break;
                        }
                    }

                    foreach (var subdata in graph.SubDatas)
                    {
                        if (!string.IsNullOrEmpty(subdata.displayName))
                            additional_terms.Add(subdata.displayName);
                    }
                }
            }

            public bool TryGatherData(out IAnalytic.IData data, [NotNullWhen(false)] out Exception error)
            {
                data = new AnalyticsData
                {
                    template_name = template_name,
                    template_path = template_path,
                    template_category = template_category,
                    template_guid = template_guid,

                    hdrp_material = hdrp_material,
                    urp_material = urp_material,
                    builtin_material = builtin_material,
                    rt_material = rt_material,

                    hdrp_vfx = hdrp_vfx,
                    urp_vfx = urp_vfx,
                    vfx_legacy = vfx_legacy,

                    additional_terms = new List<string>(additional_terms)
                };
                error = null;
                return true;
            }
        }

        public static void SendShaderGraphTemplateEvent(GraphViewTemplateDescriptor descriptor)
        {
            if (!EditorAnalytics.enabled)
                return;
            var analytic = new ShaderGraphTemplateAnalytic(descriptor);
            EditorAnalytics.SendAnalytic(analytic);
        }

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
