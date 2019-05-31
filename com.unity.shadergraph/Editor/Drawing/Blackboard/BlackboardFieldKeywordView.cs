using System;
using System.Linq;
using System.Globalization;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Toggle = UnityEngine.UIElements.Toggle;
using UnityEditor.Experimental.GraphView;

namespace UnityEditor.ShaderGraph.Drawing
{
    class BlackboardFieldKeywordView : VisualElement
    {
        readonly BlackboardField m_BlackboardField;
        readonly GraphData m_Graph;

        ShaderKeyword m_Keyword;
        Toggle m_ExposedToogle;
        TextField m_ReferenceNameField;

        static Type s_ContextualMenuManipulator = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypesOrNothing()).FirstOrDefault(t => t.FullName == "UnityEngine.UIElements.ContextualMenuManipulator");

        IManipulator m_ResetReferenceMenu;

        public delegate void OnExposedToggle();
        private OnExposedToggle m_OnExposedToggle;
        int m_UndoGroup = -1;
        
        public BlackboardFieldKeywordView(BlackboardField blackboardField, GraphData graph, ShaderKeyword keyword)
        {
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/ShaderGraphBlackboard"));
            m_BlackboardField = blackboardField;
            m_Graph = graph;
            m_Keyword = keyword;

            m_ReferenceNameField = new TextField(512, false, false, ' ');
            m_ReferenceNameField.styleSheets.Add(Resources.Load<StyleSheet>("Styles/PropertyNameReferenceField"));
            AddRow("Reference", m_ReferenceNameField);
            m_ReferenceNameField.value = keyword.referenceName;
            m_ReferenceNameField.isDelayed = true;
            m_ReferenceNameField.RegisterValueChangedCallback(newName =>
                {
                    m_Graph.owner.RegisterCompleteObjectUndo("Change Reference Name");
                    if (m_ReferenceNameField.value != m_Keyword.referenceName)
                    {
                        string newReferenceName = m_Graph.SanitizePropertyReferenceName(newName.newValue, keyword.guid);
                        keyword.overrideReferenceName = newReferenceName;
                    }
                    m_ReferenceNameField.value = keyword.referenceName;

                    if (string.IsNullOrEmpty(keyword.overrideReferenceName))
                        m_ReferenceNameField.RemoveFromClassList("modified");
                    else
                        m_ReferenceNameField.AddToClassList("modified");

                    DirtyNodes(ModificationScope.Graph);
                    UpdateReferenceNameResetMenu();
                });

            if (!string.IsNullOrEmpty(keyword.overrideReferenceName))
                m_ReferenceNameField.AddToClassList("modified");
        }

        void UpdateReferenceNameResetMenu()
        {
            if (string.IsNullOrEmpty(m_Keyword.overrideReferenceName))
            {
                this.RemoveManipulator(m_ResetReferenceMenu);
                m_ResetReferenceMenu = null;
            }
            else
            {
                m_ResetReferenceMenu = (IManipulator)Activator.CreateInstance(s_ContextualMenuManipulator, (Action<ContextualMenuPopulateEvent>)BuildContextualMenu);
                this.AddManipulator(m_ResetReferenceMenu);
            }
        }

        void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Reset Reference", e =>
                {
                    m_Keyword.overrideReferenceName = null;
                    m_ReferenceNameField.value = m_Keyword.referenceName;
                    m_ReferenceNameField.RemoveFromClassList("modified");
                    DirtyNodes(ModificationScope.Graph);
                }, DropdownMenuAction.AlwaysEnabled);
        }

        VisualElement CreateRow(string labelText, VisualElement control)
        {
            VisualElement rowView = new VisualElement();

            rowView.AddToClassList("rowView");

            Label label = new Label(labelText);

            label.AddToClassList("rowViewLabel");
            rowView.Add(label);

            control.AddToClassList("rowViewControl");
            rowView.Add(control);

            return rowView;
        }

        VisualElement AddRow(string labelText, VisualElement control)
        {
            VisualElement rowView = CreateRow(labelText, control);
            Add(rowView);
            return rowView;
        }

        void RemoveElements(VisualElement[] elements)
        {
            for (int i = 0; i < elements.Length; i++)
            {
                if (elements[i].parent == this)
                    Remove(elements[i]);
            }
        }

        void DirtyNodes(ModificationScope modificationScope = ModificationScope.Node)
        {
            foreach (var node in m_Graph.GetNodes<GraphInputNode>())
                node.Dirty(modificationScope);
        }
    }
}
