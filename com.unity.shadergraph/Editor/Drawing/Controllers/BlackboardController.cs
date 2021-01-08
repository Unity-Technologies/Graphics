using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.GraphView;

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

        public BlackboardController(BlackboardViewModel viewModel, GraphData graphData)
            : base(viewModel, graphData)
        {
        }

        protected override void ChangeModel(int changeID)
        {

        }

        protected override void ModelChanged(GraphData graphData)
        {
            base.ModelChanged(graphData);

            // Do stuff
        }
    }
}
