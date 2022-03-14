using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A GraphElement to display a <see cref="IBlackboardGraphModel"/>.
    /// </summary>
    public class Blackboard : GraphElement, IDragSource
    {
        /// <summary>
        /// The uss class name for this element.
        /// </summary>
        public static new readonly string ussClassName = "ge-blackboard";

        /// <summary>
        /// The uss class name for the windowed modifier.
        /// </summary>
        public static readonly string windowedModifierUssClassName = ussClassName.WithUssModifier("windowed");

        /// <summary>
        /// The name of the header part.
        /// </summary>
        public static readonly string blackboardHeaderPartName = "header";

        /// <summary>
        /// The name of the content part.
        /// </summary>
        public static readonly string blackboardContentPartName = "content";

        /// <summary>
        /// The persistence key for the blackboard.
        /// </summary>
        public static readonly string persistenceKey = "Blackboard";

        BlackboardViewStateComponent.GraphAssetLoadedObserver m_LoadedAssetObserver;
        BlackboardUpdateObserver m_UpdateObserver;

        Dragger m_Dragger;

        /// <summary>
        /// The Scrollview element.
        /// </summary>
        protected ScrollView m_ScrollView;

        /// <summary>
        /// The element containing the sections.
        /// </summary>
        protected VisualElement m_ContentContainer;

        bool m_Windowed;

        /// <inheritdoc />
        public override VisualElement contentContainer => m_ContentContainer;

        /// <summary>
        /// The Dragger manipulator.
        /// </summary>
        protected Dragger Dragger
        {
            get => m_Dragger;
            set
            {
                if (m_Windowed)
                    this.RemoveManipulator(m_Dragger);
                m_Dragger = value;
                if (!m_Windowed)
                    this.AddManipulator(m_Dragger);
            }
        }

        /// <summary>
        /// Whether this blackboard is its is own window or not.
        /// </summary>
        public bool Windowed
        {
            set
            {
                if (m_Windowed == value) return;

                if (value)
                {
                    AddToClassList(windowedModifierUssClassName);
                    this.RemoveManipulator(m_Dragger);
                }
                else
                {
                    RemoveFromClassList(windowedModifierUssClassName);
                    this.AddManipulator(m_Dragger);
                }
                m_Windowed = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Blackboard"/> class.
        /// </summary>
        public Blackboard()
        {
            RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
            RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);

            Dragger = new Dragger { ClampToParentEdges = true };

            RegisterCallback<DragUpdatedEvent>(e =>
            {
                e.StopPropagation();
            });

            // prevent Zoomer manipulator
            // event interception to prevent GraphView manipulators from being triggered
            // when working with the blackboard
            RegisterCallback<WheelEvent>(e =>
            {
                e.StopPropagation();
            });

            RegisterCallback<MouseDownEvent>(e =>
            {
                if (e.button == (int)MouseButton.LeftMouse)
                {
                    GraphView.Dispatch(new ClearSelectionCommand());
                }
                // prevent ContentDragger manipulator
                e.StopPropagation();
            });

            RegisterCallback<PromptSearcherEvent>(OnPromptSearcher);

            RegisterCallback<ShortcutDisplaySmartSearchEvent>(OnShortcutDisplaySmartSearchEvent);
            RegisterCallback<KeyDownEvent>(OnRenameKeyDown);

            RegisterCallback<ValidateCommandEvent>(OnValidateCommand);
            RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand);
        }

        /// <summary>
        /// Handles the <see cref="ValidateCommandEvent"/>.
        /// </summary>
        /// <param name="evt">The event.</param>
        protected virtual void OnValidateCommand(ValidateCommandEvent evt)
        {
            if (panel.GetCapturingElement(PointerId.mousePointerId) != null)
                return;

            if ((evt.commandName == GraphViewStaticBridge.EventCommandNames.Delete || evt.commandName == GraphViewStaticBridge.EventCommandNames.SoftDelete) && GraphView.CanDeleteSelection)
            {
                evt.StopPropagation();
                evt.imguiEvent?.Use();
            }
        }

        /// <summary>
        /// Handles the <see cref="ExecuteCommandEvent"/>.
        /// </summary>
        /// <param name="evt">The event.</param>
        protected virtual void OnExecuteCommand(ExecuteCommandEvent evt)
        {
            if (panel.GetCapturingElement(PointerId.mousePointerId) != null)
                return;
            if (evt.commandName == GraphViewStaticBridge.EventCommandNames.SoftDelete ||
                evt.commandName == GraphViewStaticBridge.EventCommandNames.Delete)
            {
                //TODO Tristan delete only blackboard stuff here
                GraphView.DispatchDeleteSelectionCommand();
                evt.StopPropagation();
            }

            if (evt.isPropagationStopped)
            {
                evt.imguiEvent?.Use();
            }
        }

        void RegisterObservers()
        {
            m_LoadedAssetObserver ??= new BlackboardViewStateComponent.GraphAssetLoadedObserver(GraphView.GraphTool.ToolState, GraphView.BlackboardViewState);
            GraphView.GraphTool.ObserverManager.RegisterObserver(m_LoadedAssetObserver);

            m_UpdateObserver ??= new BlackboardUpdateObserver(this, GraphView.GraphViewState, GraphView.SelectionState, GraphView.BlackboardViewState, GraphView.GraphTool.ToolState);
            GraphView.GraphTool.ObserverManager.RegisterObserver(m_UpdateObserver);
        }

        void UnregisterObservers()
        {
            GraphView.GraphTool.ObserverManager.UnregisterObserver(m_LoadedAssetObserver);
            GraphView.GraphTool.ObserverManager.UnregisterObserver(m_UpdateObserver);
        }

        // PF: remove Is..
        public override bool IsMovable()
        {
            return !m_Windowed;
        }

        /// <summary>
        /// <see cref="AttachToPanelEvent"/> event callback.
        /// </summary>
        /// <param name="e">The event.</param>
        protected virtual void OnEnterPanel(AttachToPanelEvent e)
        {
            RegisterObservers();
        }

        /// <summary>
        /// <see cref="DetachFromPanelEvent"/> event callback.
        /// </summary>
        /// <param name="e">The event.</param>
        protected virtual void OnLeavePanel(DetachFromPanelEvent e)
        {
            UnregisterObservers();
        }

        /// <inheritdoc />
        protected override void BuildPartList()
        {
            base.BuildPartList();

            PartList.AppendPart(BlackboardHeaderPart.Create(blackboardHeaderPartName, Model, this, ussClassName));
            PartList.AppendPart(BlackboardSectionListPart.Create(blackboardContentPartName, Model, this, ussClassName));
        }

        /// <inheritdoc />
        protected override void BuildElementUI()
        {
            base.BuildElementUI();

            m_ScrollView = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            m_ContentContainer = new VisualElement { name = "content-container" };

            hierarchy.Add(m_ScrollView);
            m_ScrollView.Add(m_ContentContainer);
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            // Blackboard is not really a graph element. Leaving the class meddles with the rows selection border.
            RemoveFromClassList(GraphElement.ussClassName);
            AddToClassList(ussClassName);
            this.AddStylesheet("Blackboard.uss");
            // TODO VladN: fix for light skin, remove when GTF supports light skin
            if (!EditorGUIUtility.isProSkin)
                this.AddStylesheet("Blackboard_lightFix.uss");
        }

        /// <inheritdoc />
        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);

            var selectedModels = GetSelection();
            if (selectedModels.Count > 0)
            {
                evt.menu.AppendAction("Delete", _ =>
                {
                    GraphView.Dispatch(new DeleteElementsCommand(selectedModels));
                }, _ => DropdownMenuAction.Status.Normal);
            }
        }

        /// <summary>
        /// Returns the selection.
        /// </summary>
        /// <returns>The selection.</returns>
        public IReadOnlyList<IGraphElementModel> GetSelection() => GraphView.GetSelection();

        /// <summary>
        /// Callback for the ShortcutDisplaySmartSearchEvent.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnShortcutDisplaySmartSearchEvent(ShortcutDisplaySmartSearchEvent e)
        {
            using (var promptSearcherEvent = PromptSearcherEvent.GetPooled(e.MousePosition))
            {
                promptSearcherEvent.target = e.target;
                SendEvent(promptSearcherEvent);
            }
            e.StopPropagation();
        }

        /// <summary>
        /// Callback for the KeyDownEvent to handle renames.
        /// </summary>
        /// <param name="e">The event.</param>
        protected new void OnRenameKeyDown(KeyDownEvent e)
        {
            if (e.target == this)
            {
                // Forward event to the last selected element.
                var renamableSelection = GraphView.GetSelection().Where(x => x.IsRenamable());
                var lastSelectedItem = renamableSelection.LastOrDefault();
                var lastSelectedItemUI = lastSelectedItem?.GetUI<GraphElement>(View);

                lastSelectedItemUI?.OnRenameKeyDown(e);
            }
        }

        void OnPromptSearcher(PromptSearcherEvent e)
        {
            var graphModel = (Model as IBlackboardGraphModel)?.GraphModel;

            if (graphModel == null)
            {
                return;
            }

            SearcherService.ShowVariableTypes(
                (Stencil)Model.GraphModel.Stencil,
                View.GraphTool.Preferences,
                e.MenuPosition,
                (t, _) =>
                {
                    GraphView.Dispatch(new CreateGraphVariableDeclarationCommand
                    {
                        VariableName = "newVariable",
                        TypeHandle = t,
                        ModifierFlags = ModifierFlags.None,
                        IsExposed = true
                    });
                });

            e.StopPropagation();
        }
    }
}
