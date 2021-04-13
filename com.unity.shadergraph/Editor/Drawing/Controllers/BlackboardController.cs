using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.GraphView;

namespace UnityEditor.ShaderGraph.Drawing.Views.Blackboard
{
    class BlackboardController : SGViewController<BlackboardViewModel>
    {
        IList<SGBlackboardSection> m_BlackboardSections;

        public BlackboardController(BlackboardViewModel viewModel, GraphData graphData)
            : base(viewModel, graphData)
        {
        }

        protected override void ModelChanged(GraphData graphData)
        {
            throw new System.NotImplementedException();
        }
    }
}
