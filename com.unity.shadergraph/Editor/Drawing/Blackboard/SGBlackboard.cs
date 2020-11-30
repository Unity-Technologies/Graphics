using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Views
{
    class SGBlackboard : GraphSubWindow, ISelection
    {
        private Button m_AddButton;
        private Dragger m_Dragger;
        protected override string windowTitle => "Blackboard";
        protected override string elementName => "SGBlackboard";
        protected override string styleName => "Blackboard";
        protected override string UxmlName => "GraphView/Blackboard";
        protected override string layoutKey => "UnityEditor.ShaderGraph.Blackboard";

        public Action<SGBlackboard> addItemRequested { get; set; }
        public Action<SGBlackboard, int, VisualElement> moveItemRequested { get; set; }
        public Action<SGBlackboard, VisualElement, string> editTextRequested { get; set; }

        public SGBlackboard(GraphView associatedGraphView) : base(associatedGraphView)
        {
            windowDockingLayout.dockingLeft = true;

            m_AddButton = m_MainContainer.Q(name: "addButton") as Button;
            m_AddButton.clickable.clicked += () => {
                if (addItemRequested != null)
                {
                    addItemRequested(this);
                }
            };

            isWindowScrollable = true;

            RegisterCallback<ValidateCommandEvent>(OnValidateCommand);
            RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand);

            focusable = true;
        }

        public virtual void AddToSelection(ISelectable selectable)
        {
            graphView?.AddToSelection(selectable);
        }

        public virtual void RemoveFromSelection(ISelectable selectable)
        {
            graphView?.RemoveFromSelection(selectable);
        }

        public virtual void ClearSelection()
        {
            graphView?.ClearSelection();
        }

        private void OnValidateCommand(ValidateCommandEvent evt)
        {
            int x = 0;
            x++;

            //graphView?.OnValidateCommand(evt);
        }

        private void OnExecuteCommand(ExecuteCommandEvent evt)
        {
            int x = 0;
            x++;
            //graphView?.OnExecuteCommand(evt);
        }
    }
}
