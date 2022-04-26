using System.Collections.Generic;
using System.Linq;
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

        protected internal static Vector2 ComputeSubgraphNodePosition(List<IGraphElementModel> elements, GraphView graphView)
        {
            var firstElement = elements.FirstOrDefault().GetView(graphView);

            if (firstElement != null)
            {
                var elementsUIWithoutEdges = elements.Where(e => !(e is IEdgeModel)).Select(n => n.GetView(graphView)).Where(e => e != null);
                var encompassingRect = elementsUIWithoutEdges.Aggregate(firstElement.layout, (current, e) => RectUtils.Encompass(current, e.layout));

                return encompassingRect.center;
            }

            return Vector2.zero;
        }

        protected override void BuildPartList()
        {
            base.BuildPartList();
            PartList.ReplacePart(titleIconContainerPartName, SubgraphIconTitleProgressPart.Create(titleIconContainerPartName, Model, this, ussClassName));
        }

        void OnOpenSubgraph()
        {
            if (Model is ISubgraphNodeModel subgraphNodeModel && subgraphNodeModel.SubgraphModel != null)
            {
                GraphView.Dispatch(new LoadGraphCommand(subgraphNodeModel.SubgraphModel, null, LoadGraphCommand.LoadStrategies.PushOnStack));
                GraphView.Window.UpdateWindowsWithSameCurrentGraph(false);
            }
        }

        /// <inheritdoc/>
        protected override void PostBuildUI()
        {
            base.PostBuildUI();
            AddToClassList(ussClassName);
        }
    }
}
