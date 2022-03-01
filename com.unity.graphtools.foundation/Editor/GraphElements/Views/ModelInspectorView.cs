using System;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A view to display the model inspector.
    /// </summary>
    public class ModelInspectorView : BaseView
    {
        class ModelInspectorObserver : StateObserver
        {
            ModelInspectorView m_Inspector;
            GraphViewStateComponent m_GraphViewStateComponent;

            public ModelInspectorObserver(ModelInspectorView inspector, GraphViewStateComponent graphViewState)
                : base(inspector.ModelInspectorState, graphViewState)
            {
                m_Inspector = inspector;
                m_GraphViewStateComponent = graphViewState;
            }

            public override void Observe()
            {
                if (m_Inspector?.panel != null)
                    m_Inspector.Update(this, m_GraphViewStateComponent);
            }
        }

        public static readonly string ussClassName = "model-inspector";
        public static readonly string titleUssClassName = ussClassName.WithUssElement("title");
        public static readonly string containerUssClassName = ussClassName.WithUssElement("container");

        ModelInspectorObserver m_ModelObserver;

        Label m_SidePanelTitle;
        VisualElement m_SidePanelInspectorContainer;
        ModelUI m_Inspector;
        GraphView m_AssociatedGraphView;

        /// <inheritdoc />
        public override ICommandTarget Parent => m_AssociatedGraphView as ICommandTarget ?? GraphTool;

        /// <summary>
        /// The model inspector state component.
        /// </summary>
        public ModelInspectorStateComponent ModelInspectorState { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelInspectorView"/> class.
        /// </summary>
        /// <param name="graphTool">The <see cref="BaseGraphTool"/> of the view.</param>
        /// <param name="associatedGraphView">The graph view associated with this inspector.</param>
        public ModelInspectorView(BaseGraphTool graphTool, GraphView associatedGraphView)
        : base(graphTool)
        {
            ModelInspectorState = new ModelInspectorStateComponent();
            GraphTool.State.AddStateComponent(ModelInspectorState);

            m_AssociatedGraphView = associatedGraphView;

            m_ModelObserver = new ModelInspectorObserver(this, m_AssociatedGraphView.GraphViewState);

            RegisterCommandHandler<SetModelFieldCommand>(SetModelFieldCommand.DefaultCommandHandler);

            AddToClassList(ussClassName);

            m_SidePanelTitle = new Label();
            m_SidePanelTitle.AddToClassList(titleUssClassName);
            Add(m_SidePanelTitle);

            m_SidePanelInspectorContainer = new VisualElement();
            m_SidePanelInspectorContainer.AddToClassList(containerUssClassName);
            Add(m_SidePanelInspectorContainer);

            RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
            RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);

            this.AddStylesheet("ModelInspector.uss");
        }

        public void RegisterCommandHandler<TCommand>(CommandHandler<UndoStateComponent, GraphViewStateComponent, TCommand> commandHandler)
            where TCommand : ICommand
        {
            Dispatcher.RegisterCommandHandler(commandHandler, GraphTool.UndoStateComponent, m_AssociatedGraphView.GraphViewState);
        }

        /// <summary>
        /// Callback for the <see cref="AttachToPanelEvent"/>.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnEnterPanel(AttachToPanelEvent e)
        {
            GraphTool?.ObserverManager?.RegisterObserver(m_ModelObserver);
        }

        /// <summary>
        /// Callback for the <see cref="DetachFromPanelEvent"/>.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnLeavePanel(DetachFromPanelEvent e)
        {
            GraphTool?.ObserverManager?.UnregisterObserver(m_ModelObserver);
        }

        void Update(IStateObserver observer, GraphViewStateComponent graphViewStateComponent)
        {
            using (var observation = observer.ObserveState(ModelInspectorState))
            {
                var rebuildType = observation.UpdateType;

                if (rebuildType == UpdateType.Complete)
                {
                    m_SidePanelInspectorContainer.Clear();
                    if (ModelInspectorState.EditedNode != null)
                    {
                        m_SidePanelTitle.text = (ModelInspectorState.EditedNode as IHasTitle)?.Title ?? "Node Inspector";
                        m_Inspector = GraphElementFactory.CreateUI<ModelUI>(this, ModelInspectorState.EditedNode);
                        if (m_Inspector != null)
                            m_SidePanelInspectorContainer.Add(m_Inspector);
                    }
                    else
                    {
                        m_Inspector = null;
                        m_SidePanelTitle.text = "Node Inspector";
                    }
                }
            }

            using (var gvObservation = observer.ObserveState(graphViewStateComponent))
            {
                var rebuildType = gvObservation.UpdateType;

                if (rebuildType != UpdateType.None)
                {
                    m_SidePanelTitle.text = (ModelInspectorState.EditedNode as IHasTitle)?.Title ?? "Node Inspector";
                    m_Inspector?.UpdateFromModel();
                }
            }
        }
    }
}
