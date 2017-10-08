using System;
using System.Collections.Generic;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    public class LayerWeightsOutputNode : AbstractMaterialNode, IOnAssetEnabled
    {
        public LayerWeightsOutputNode()
        {
            name = "LayerWeights";
        }

        public override bool allowedInRemapGraph { get; } = false;
        public override bool allowedInSubGraph { get; } = false;

        public void OnEnable()
        {
            var layeredGraph = owner as LayeredShaderGraph;
            if (layeredGraph == null)
                return;

            var goodSlots =  new List<int>();
            foreach (var layer in layeredGraph.layers)
            {
                AddSlot(new MaterialSlot(layer.guid.GetHashCode(), LayeredShaderGraph.LayerToFunctionName(layer.guid), LayeredShaderGraph.LayerToFunctionName(layer.guid), SlotType.Input, SlotValueType.Vector1, new Vector4(0, 0, 0, 0)));
                goodSlots.Add(layer.guid.GetHashCode());
            }

            RemoveSlotsNameNotMatching(goodSlots);
        }
    }
}
