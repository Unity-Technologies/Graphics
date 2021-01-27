using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    interface ISGViewModel
    {
        VisualElement ParentView { get; set; }

		// TODO: Should this be readonly? or otherwise const-marked in some way to prevent modification except by IGraphDataAction
        GraphData Model { get; set; }

        // Wipes all data in this view-model
        void Reset();
    }
}
