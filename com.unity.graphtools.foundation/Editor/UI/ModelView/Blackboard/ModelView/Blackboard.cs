using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A GraphElement to display a <see cref="IBlackboardGraphModel"/>.
    /// </summary>
    public class Blackboard : BlackboardElement
    {
        /// <summary>
        /// The uss class name for this element.
        /// </summary>
        public static new readonly string ussClassName = "ge-blackboard";

        /// <summary>
        /// The name of the header part.
        /// </summary>
        public static readonly string blackboardHeaderPartName = "header";

        /// <summary>
        /// The name of the content part.
        /// </summary>
        public static readonly string blackboardContentPartName = "content";

        /// <summary>
        /// The element containing the sections.
        /// </summary>
        protected VisualElement m_ContentContainer;

        /// <inheritdoc />
        public override VisualElement contentContainer => m_ContentContainer;

        /// <summary>
        /// The ScrollView used for the whole blackboard.
        /// </summary>
        public ScrollView ScrollView { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Blackboard"/> class.
        /// </summary>
        public Blackboard()
        {
            RegisterCallback<DragUpdatedEvent>(e =>
            {
                e.StopPropagation();
            });

            RegisterCallback<MouseDownEvent>(e =>
            {
                if (e.button == (int)MouseButton.LeftMouse)
                {
                    BlackboardView.Dispatch(new ClearSelectionCommand());
                }
                e.StopPropagation();
            });

            RegisterCallback<PromptSearcherEvent>(OnPromptSearcher);
            RegisterCallback<ShortcutDisplaySmartSearchEvent>(OnShortcutDisplaySmartSearchEvent);
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

            ScrollView = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            m_ContentContainer = new VisualElement { name = "content-container" };

            hierarchy.Add(ScrollView);
            ScrollView.Add(m_ContentContainer);
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            AddToClassList(ussClassName);
            this.AddStylesheet("Blackboard.uss");

            var headerPart = PartList.GetPart(blackboardHeaderPartName).Root;
            if (headerPart != null)
                hierarchy.Insert(0, headerPart);
        }

        /// <inheritdoc />
        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);

            BlackboardView.ViewSelection.BuildContextualMenu(evt);

            evt.menu.AppendAction("Select Unused", _ =>
            {
                BlackboardView.DispatchSelectUnusedVariables();
            }, _ => DropdownMenuAction.Status.Normal);
        }

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

        void OnPromptSearcher(PromptSearcherEvent e)
        {
            var graphModel = (Model as IBlackboardGraphModel)?.GraphModel;

            if (graphModel == null)
            {
                return;
            }

            SearcherService.ShowVariableTypes(
                (Stencil)graphModel.Stencil,
                RootView.GraphTool.Preferences,
                e.MenuPosition,
                (t, _) =>
                {
                    BlackboardView.Dispatch(new CreateGraphVariableDeclarationCommand
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
