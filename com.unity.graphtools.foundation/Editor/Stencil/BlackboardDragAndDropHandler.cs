using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Handles Drag and Drop of Blackboard Elements.
    /// Create a variable based on the Blackboard Field dragged
    /// </summary>
    public class BlackboardDragAndDropHandler : DragAndDropHandler
    {
        const float DragDropSpacer = 25f;

        protected IDragSource DragSource { get; }
        protected IModelView View { get; }

        List<(IVariableDeclarationModel e1, SerializableGUID, Vector2)> m_VariablesToCreate;
        bool m_DropAuthorized;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlackboardDragAndDropHandler"/> class.
        /// </summary>
        /// <param name="graphView">The graph view used as the drag source.</param>
        public BlackboardDragAndDropHandler(GraphView graphView)
            : this(graphView, graphView)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlackboardDragAndDropHandler"/> class.
        /// </summary>
        /// <param name="blackboard">The blackboard used as the drag source.</param>
        public BlackboardDragAndDropHandler(Blackboard blackboard)
            : this(blackboard, blackboard.GraphView)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlackboardDragAndDropHandler"/> class.
        /// </summary>
        /// <param name="dragSource">The drag source.</param>
        /// <param name="view">The view.</param>
        public BlackboardDragAndDropHandler(IDragSource dragSource, IModelView view)
        {
            DragSource = dragSource;
            View = view;
        }

        /// <inheritdoc />
        public override void OnDragUpdated(DragUpdatedEvent e)
        {
            if (!m_DropAuthorized)
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
            else
                DragAndDrop.visualMode = e.ctrlKey ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Move;
        }

        /// <inheritdoc />
        public override void OnDragEnter(DragEnterEvent e)
        {
            base.OnDragEnter(e);

            if (m_VariablesToCreate != null)
                return;

            var dropElements = DragSource.GetSelection();

            var contentViewContainer = (e.target as GraphView)?.ContentViewContainer ?? e.target as VisualElement;

            var droppedNodes = dropElements.OfType<INodeModel>();

            var droppedVariables = dropElements
                .OfType<IVariableDeclarationModel>()
                .Select((e1, i) => (
                    e1,
                    GUID.Generate().ToSerializableGUID(),
                    contentViewContainer.WorldToLocal(e.mousePosition) + i * DragDropSpacer * Vector2.down))
                .ToList();

            if (droppedNodes.Any(e2 => !(e2 is IVariableNodeModel)) && droppedVariables.Any())
            {
                // no way to handle this ATM
                throw new ArgumentException(
                    "Unhandled case, dragging blackboard/variables fields and nodes at the same time");
            }

            var graphModel = View.GraphTool.ToolState.GraphModel;
            if (graphModel.Stencil is Stencil stencil)
            {
                m_VariablesToCreate = droppedVariables
                    .Where(v => stencil.CanCreateVariableInGraph(v.Item1, graphModel))
                    .ToList();
                new List<(IVariableDeclarationModel, SerializableGUID, Vector2)>(droppedVariables.Count);

                m_DropAuthorized = m_VariablesToCreate.Any();
            }
        }

        /// <inheritdoc />
        public override void OnDragExited(DragExitedEvent e)
        {
            base.OnDragExited(e);
            m_VariablesToCreate = null;
        }

        /// <inheritdoc />
        public override void OnDragPerform(DragPerformEvent e)
        {
            if (m_VariablesToCreate?.Any() == true)
            {
                var command = new CreateNodeCommand();
                foreach (var (model, _, position) in m_VariablesToCreate)
                {
                    command.WithNodeOnGraph(model, position);
                }
                View.Dispatch(command);
                m_VariablesToCreate = null;
            }
        }
    }
}
