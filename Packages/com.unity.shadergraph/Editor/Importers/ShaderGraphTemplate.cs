using System;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    struct ShaderGraphTemplate
    {
        public string name;
        public string category;
        [Multiline]
        public string description;
        public Texture2D icon;
        public Texture2D thumbnail;
        public int order;

        internal static Experimental.GraphView.DataBag GatherSearchTerms(GraphData graph)
        {
            // clear the existing terms, as this object's serialization persists across imports.
            var additionalSearchTerms = default(Experimental.GraphView.DataBag);

            if (graph is null || graph.objectIdIsEmpty)
                return additionalSearchTerms;

            foreach (var target in graph.activeTargets)
            {
                if (target is null or MultiJsonInternal.UnknownTargetType || target.objectIdIsEmpty || target.activeSubTarget is null || target.activeSubTarget.objectIdIsEmpty)
                    continue;

                additionalSearchTerms.AddCustomData("RenderPipeline", target.displayName);
                additionalSearchTerms.AddCustomData("Material", target.activeSubTarget.displayName);
                additionalSearchTerms.AddCustomData("VFX", target.SupportsVFX() ? "Supported" : "Not Supported");

                if (target.SearchTerms != null)
                    foreach (var term in target.SearchTerms)
                        additionalSearchTerms.AddCustomData(term.key, term.value);

                if (target.activeSubTarget.SearchTerms != null)
                    foreach (var term in target.activeSubTarget.SearchTerms)
                        additionalSearchTerms.AddCustomData(term.key, term.value);
            }

            foreach (var subData in graph.SubDatas)
                if (subData == null || subData is MultiJsonInternal.UnknownGraphDataExtension || subData.objectIdIsEmpty || string.IsNullOrEmpty(subData.displayName))
                    continue;
                else additionalSearchTerms.AddCustomData("DataExtension", subData.displayName);

            return additionalSearchTerms;
        }
    }
}
