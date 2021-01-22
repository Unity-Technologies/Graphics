using UnityEngine;
using System;
using UnityEditor.ShaderGraph.Internal;
using GraphDataStore = UnityEditor.ShaderGraph.DataStore<UnityEditor.ShaderGraph.GraphData>;

namespace UnityEditor.ShaderGraph.Drawing
{
    class ShaderInputViewController : SGViewController<ShaderInput, ShaderInputViewModel>
    {
        internal ShaderInputViewController(ShaderInput model, ShaderInputViewModel inViewModel, GraphDataStore graphDataStore)
            : base(model, inViewModel, graphDataStore)
        {

        }

        protected override void RequestModelChange(IGraphDataAction changeAction)
        {
            DataStore.Dispatch(changeAction);
        }

        // Called by GraphDataStore.Subscribe after the model has been changed
        protected override void ModelChanged(GraphData graphData, IGraphDataAction changeAction) { }

        public override void ApplyChanges() { }
    }
}
