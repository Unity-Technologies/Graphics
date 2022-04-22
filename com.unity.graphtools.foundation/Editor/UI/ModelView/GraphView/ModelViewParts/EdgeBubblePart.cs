using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A part to build the UI for a text bubble on an edge.
    /// </summary>
    public class EdgeBubblePart : BaseModelViewPart
    {
        public static readonly string ussClassName = "ge-edge-bubble-part";

        /// <summary>
        /// Creates a new instance of the <see cref="EdgeBubblePart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="EdgeBubblePart"/>.</returns>
        public static EdgeBubblePart Create(string name, IModel model, IModelView ownerElement, string parentClassName)
        {
            if (model is IEdgeModel)
            {
                return new EdgeBubblePart(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        protected EdgeBubble m_EdgeBubble;

        /// <inheritdoc />
        public override VisualElement Root => m_EdgeBubble;

        /// <summary>
        /// Initializes a new instance of the <see cref="EdgeBubblePart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        protected EdgeBubblePart(string name, IModel model, IModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) {}

        /// <inheritdoc />
        protected override void BuildPartUI(VisualElement container)
        {
            m_EdgeBubble = new EdgeBubble { name = PartName };
            m_EdgeBubble.AddToClassList(ussClassName);
            m_EdgeBubble.AddToClassList(m_ParentClassName.WithUssElement(PartName));
            container.Add(m_EdgeBubble);
        }

        /// <inheritdoc />
        protected override void PostBuildPartUI()
        {
            base.PostBuildPartUI();
            m_EdgeBubble.AddStylesheet("EdgeBubblePart.uss");
        }

        internal static bool EdgeShouldShowLabel(IEdgeModel model, SelectionStateComponent selectionStateComponent)
        {
            return model.FromPort is IReorderableEdgesPortModel reorderablePort
                   && reorderablePort.HasReorderableEdges
                   && model.FromPort.GetConnectedEdges().Count > 1
                   && (selectionStateComponent.IsSelected(model.FromPort.NodeModel)
                       || model.FromPort.GetConnectedEdges().Any(selectionStateComponent.IsSelected));
        }

        /// <inheritdoc />
        protected override void UpdatePartFromModel()
        {
            if (!(m_Model is IEdgeModel edgeModel) || !(m_OwnerElement is Edge edge))
                return;

            if (edge.GraphView?.GraphViewModel != null && EdgeShouldShowLabel(edgeModel, edge.GraphView.GraphViewModel.SelectionState))
            {
                var attachPoint = edge.EdgeControl as VisualElement ?? edge;
                m_EdgeBubble.SetAttacherOffset(Vector2.zero);
                m_EdgeBubble.text = edgeModel.EdgeLabel;
                m_EdgeBubble.AttachTo(attachPoint, SpriteAlignment.Center);
                m_EdgeBubble.style.visibility = StyleKeyword.Null;
            }
            else
            {
                m_EdgeBubble.Detach();
                m_EdgeBubble.style.visibility = Visibility.Hidden;
            }
        }

    }
}
