using System.Collections.Generic;
using UnityEngine;
using Unity.GraphCommon.LowLevel.Editor;

namespace UnityEditor.VFX
{
    internal class AttributeLayoutPass : CompilationPass
    {
        public bool Execute(ref CompilationContext context)
        {
            // Gather all attribute set datas
            // For each attribute set :
            //  Gather attributes
            //  Detect local-only attributes, else stored
            //  Compute affinities between stored attributes
            //  Group attributes based on affinities
            //  Generate attribute layout description

            var attributeSetLayoutCompilationData = context.data.GetOrCreate<AttributeSetLayoutCompilationData>();

            var traverser = context.graph.CreateTraverser();
            foreach (var dataView in context.graph.DataViews)
            {
                if (dataView.DataDescription is AttributeData attributeData)
                    attributeSetLayoutCompilationData[attributeData] = GenerateAttributeLayout(traverser, dataView, context.graph);
            }
            return true;
        }

        private static AttributeSetLayout GenerateAttributeLayout(GraphTraverser traverser, DataView attributeSetDataView, IReadOnlyGraph graph)
        {
            Dictionary<DataViewId, long> attributeKeys = new Dictionary<DataViewId, long>();
            HashSet<DataViewId> storedAttributes = new HashSet<DataViewId>();

            foreach (var rootDataNode in traverser.TraverseDataRoots())
            {
                if (rootDataNode.DataContainer.RootDataView.DataDescription != attributeSetDataView.Root.DataDescription)
                    continue;

                foreach (var dataNode in traverser.TraverseDataDownwards(rootDataNode))
                {
                    foreach (var dataView in dataNode.UsedDataViews)
                    {
                        if (!dataView.Parent.HasValue || !dataView.Parent.Value.Id.Equals(attributeSetDataView.Id)) continue;

                        attributeKeys.TryAdd(dataView.Id, 0);

                        bool isRead = dataNode.Reads(dataView.Id);
                        bool isWritten = dataNode.Writes(dataView.Id);

                        long readWriteValue = 0;
                        if (isRead)
                            readWriteValue |= 0x01;
                        if (isWritten)
                            readWriteValue |= 0x02;
                        int shift = dataNode.TaskNode.Id.Index << 1;
                        attributeKeys[dataView.Id] |= readWriteValue << shift;
                        if (isRead)
                        {
                            if (isWritten)
                            {
                                storedAttributes.Add(dataView.Id);
                                continue;
                            }
                            foreach (var parentDataNode in traverser.TraverseDataUpwards(dataNode))
                            {
                                if(dataNode.Id.Equals(parentDataNode.Id))
                                    continue;
                                if (parentDataNode.Writes(dataView.Id))
                                {
                                    storedAttributes.Add(dataView.Id);
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            var attributeGroups = new Dictionary<long, List<Attribute>>();

            foreach (var kvp in attributeKeys)
            {
                var dataViewId = kvp.Key;
                if (storedAttributes.Contains(dataViewId))
                {
                    if (!attributeGroups.TryGetValue(kvp.Value, out var attributeGroup))
                    {
                        attributeGroup = new List<Attribute>();
                        attributeGroups.Add(kvp.Value, attributeGroup);
                    }

                    AttributeKey attributeKey = graph.DataViews[dataViewId].SubDataKey as AttributeKey;
                    if (attributeKey == null)
                    {
                        Debug.LogWarning($"Attribute {dataViewId} does not have an AttributeKey as sub data key");
                    }
                    Attribute attribute = attributeKey.Attribute;

                    attributeGroup.Add(attribute);
                }
            }
            var attributeDataDesc = attributeSetDataView.DataDescription as AttributeData;

            bool isParticleAttributeSet = attributeSetDataView.Parent is { DataDescription: ParticleData };
            uint capacity = isParticleAttributeSet ? VfxDispatch.AlignToThreadGroups(attributeDataDesc.Capacity) : attributeDataDesc.Capacity;
            var attributeSetLayout = new AttributeSetLayout(capacity);

            foreach (var group in attributeGroups)
            {
                attributeSetLayout.AddGroup(group.Value);
            }
            return attributeSetLayout;
        }
    }
}
