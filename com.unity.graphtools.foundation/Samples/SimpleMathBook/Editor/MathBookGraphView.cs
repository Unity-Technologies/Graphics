using System;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.UIElements;
#if !UNITY_2022_2_OR_NEWER
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
#endif

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    class MathBookGraphView : GraphView
    {
#if !UNITY_2022_2_OR_NEWER
        class ResultObserver : StateObserver
        {
            GraphProcessingStateComponent m_GraphProcessingState;
            Label m_ResultLabel;

            public ResultObserver(GraphProcessingStateComponent graphProcessingState, Label resultLabel)
                : base(graphProcessingState)
            {
                m_GraphProcessingState = graphProcessingState;
                m_ResultLabel = resultLabel;
            }

            /// <inheritdoc />
            public override void Observe()
            {
                var results = m_GraphProcessingState.RawResults?.OfType<MathBookProcessingResults>().FirstOrDefault();
                var resultString = results?.EvaluationResult;
                m_ResultLabel.text = $"Result: {resultString ?? "---"}";
            }
        }

        ResultObserver m_ResultObserver;
        Label m_ResultLabel;
#endif

        /// <inheritdoc />
        public MathBookGraphView(GraphViewEditorWindow window, BaseGraphTool graphTool, string graphViewName,
            GraphViewDisplayMode displayMode = GraphViewDisplayMode.Interactive)
            : base(window, graphTool, graphViewName, displayMode)
        {
#if !UNITY_2022_2_OR_NEWER
            if (displayMode == GraphViewDisplayMode.Interactive)
            {
                m_ResultLabel = new Label();

                m_ResultLabel.style.position = Position.Absolute;
                m_ResultLabel.style.left = 0;
                m_ResultLabel.style.bottom = 0;
                m_ResultLabel.style.width = 200;
                m_ResultLabel.style.color = new Color(0, 0, 0);
                m_ResultLabel.style.backgroundColor = new Color(200, 200, 200);

                Add(m_ResultLabel);
            }
#endif
        }

        /// <inheritdoc />
        protected override void RegisterObservers()
        {
            base.RegisterObservers();

#if !UNITY_2022_2_OR_NEWER
            if (GraphTool?.ObserverManager == null)
                return;

            m_ResultObserver ??= new ResultObserver(GraphTool.GraphProcessingState, m_ResultLabel);
            GraphTool.ObserverManager.RegisterObserver(m_ResultObserver);
#endif
        }

        /// <inheritdoc />
        protected override void UnregisterObservers()
        {
            base.UnregisterObservers();

#if !UNITY_2022_2_OR_NEWER
            if (GraphTool?.ObserverManager == null)
                return;

            GraphTool.ObserverManager.UnregisterObserver(m_ResultObserver);
#endif
        }

        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.menu.MenuItems().Count > 0)
                evt.menu.AppendSeparator();

            var selection = GetSelection().ToList();

            if (selection.Any(element => element is INodeModel || element is IPlacematModel || element is IStickyNoteModel))
            {
                evt.menu.AppendAction("Create MathBook Subgraph", _ =>
                {
                    var template = new GraphTemplate<MathBookStencil>("MathBook Subgraph");
                    Dispatch(new CreateSubgraphCommand(typeof(MathBookAsset), selection, template, this));
                }, selection.Count == 0 ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);
            }

            base.BuildContextualMenu(evt);
        }

        public override void BuildOptionMenu(GenericMenu menu)
        {
            base.BuildOptionMenu(menu);

            var preferences = GraphTool?.Preferences;

            GUIContent CreateTextContent(string content)
            {
                // TODO: Replace by EditorGUIUtility.TrTextContent when it's made 'public'.
                return new GUIContent(content);
            }

            void MenuItem(string title, bool value, GenericMenu.MenuFunction onToggle)
                => menu.AddItem(CreateTextContent(title), value, onToggle);

            void MenuToggle(string title, BoolPref k, Action callback = null)
            {
                if (preferences != null)
                    MenuItem(title, preferences.GetBool(k), () =>
                    {
                        preferences.ToggleBool(k);
                        callback?.Invoke();
                    });
            }

            menu.AddSeparator("");
            MenuToggle("Auto Itemize Constants", BoolPref.AutoItemizeConstants);
            MenuToggle("Auto Itemize Variables", BoolPref.AutoItemizeVariables);
        }
    }
}
