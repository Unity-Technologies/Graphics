using System;
using System.Collections.Generic;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class LayerWeightsOutputNode : AbstractMaterialNode, IOnAssetEnabled
    {
        public LayerWeightsOutputNode()
        {
            name = "LayerWeights";
        }

        public override bool allowedInRemapGraph { get { return false; } }
        public override bool allowedInSubGraph { get { return false; } }

        public void OnEnable()
        {
            var layeredGraph = owner as LayeredShaderGraph;
            if (layeredGraph == null)
                return;

            var goodSlots =  new List<int>();
            foreach (var layer in layeredGraph.layers)
            {
                AddSlot(new Vector1MaterialSlot(layer.guid.GetHashCode(), LayeredShaderGraph.LayerToFunctionName(layer.guid), LayeredShaderGraph.LayerToFunctionName(layer.guid), SlotType.Input, 0));
                goodSlots.Add(layer.guid.GetHashCode());
            }

            RemoveSlotsNameNotMatching(goodSlots);
        }
    }
}
