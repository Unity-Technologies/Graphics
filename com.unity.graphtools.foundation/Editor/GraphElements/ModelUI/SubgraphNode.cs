using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// UI for a <see cref="ISubgraphNodeModel"/>.
    /// </summary>
    public class SubgraphNode : CollapsibleInOutNode
    {
        public SubgraphNode()
        {
            var clickable = new Clickable(OnOpenSubgraph);
            clickable.activators.Clear();
            clickable.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, clickCount = 2 });
            this.AddManipulator(clickable);
        }

        protected override void BuildPartList()
        {
            base.BuildPartList();
            PartList.ReplacePart(titleIconContainerPartName, SubgraphIconTitleProgressPart.Create(titleIconContainerPartName, Model, this, ussClassName));
        }

        void OnOpenSubgraph()
        {
            if (Model is ISubgraphNodeModel subgraphNodeModel && subgraphNodeModel.ReferenceGraphAssetModel != null)
                GraphView.Dispatch(new LoadGraphAssetCommand(subgraphNodeModel.ReferenceGraphAssetModel, null, LoadGraphAssetCommand.LoadStrategies.PushOnStack));
        }
    }
}
