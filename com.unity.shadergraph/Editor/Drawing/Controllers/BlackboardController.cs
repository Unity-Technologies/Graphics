using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.GraphView;

using GraphDataStore = UnityEditor.ShaderGraph.DataStore<UnityEditor.ShaderGraph.GraphData>;

namespace UnityEditor.ShaderGraph.Drawing
{
    class BlackboardController : SGViewController<BlackboardViewModel>
    {
        public class Changes
        {
            public const int AddBlackboardItem = 0;
            public const int RemoveBlackboardItem = 1;
        }
        IList<SGBlackboardSection> m_BlackboardSections;

        public BlackboardController(BlackboardViewModel viewModel, GraphDataStore graphDataStore)
            : base(viewModel, graphDataStore)
        {
        }

        protected override void ChangeModel(int changeID)
        {

        }

        public override void ApplyChanges()
        {

        }

        public override void ModelChanged(GraphData graphData)
        {
            base.ModelChanged(graphData);


            // Do stuff
        }
    }
}
