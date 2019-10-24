using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph.Drawing
{
    abstract class BlackboardFieldView : VisualElement
    {
        readonly BlackboardField m_BlackboardField;
        readonly GraphData m_Graph;
        public GraphData graph => m_Graph;

        ShaderInput m_Input;

        Toggle m_ExposedToogle;
        TextField m_ReferenceNameField;
        List<VisualElement> m_Rows;
        public List<VisualElement> rows => m_Rows;

        int m_UndoGroup = -1;
        public int undoGroup => m_UndoGroup;

        static Type s_ContextualMenuManipulator = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypesOrNothing()).FirstOrDefault(t => t.FullName == "UnityEngine.UIElements.ContextualMenuManipulator");
        IManipulator m_ResetReferenceMenu;

        EventCallback<KeyDownEvent> m_KeyDownCallback;
        public EventCallback<KeyDownEvent> keyDownCallback => m_KeyDownCallback;
        EventCallback<FocusOutEvent> m_FocusOutCallback;
        public EventCallback<FocusOutEvent> focusOutCallback => m_FocusOutCallback;

        public BlackboardFieldView(BlackboardField blackboardField, GraphData graph, ShaderInput input)
        {
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/ShaderGraphBlackboard"));
            m_BlackboardField = blackboardField;
            m_Graph = graph;
            m_Input = input;
            m_Rows = new List<VisualElement>();

            m_KeyDownCallback = new EventCallback<KeyDownEvent>(evt =>
            {
                // Record Undo for input field edit
                if (m_UndoGroup == -1)
                {
                    m_UndoGroup = Undo.GetCurrentGroup();
                    graph.owner.RegisterCompleteObjectUndo("Change property value");
                }
                // Handle scaping input field edit
                if (evt.keyCode == KeyCode.Escape && m_UndoGroup > -1)
                {
                    Undo.RevertAllDownToGroup(m_UndoGroup);
                    m_UndoGroup = -1;
                    evt.StopPropagation();
                }
                // Dont record Undo again until input field is unfocused
                m_UndoGroup++;
                this.MarkDirtyRepaint();
            });

            m_FocusOutCallback = new EventCallback<FocusOutEvent>(evt =>
            {
                // Reset UndoGroup when done editing input field
                m_UndoGroup = -1;
            });

            BuildDefaultFields(input);
            BuildCustomFields(input);

            AddToClassList("sgblackboardFieldView");
        }

        void BuildDefaultFields(ShaderInput input)
        {
            if(!m_Graph.isSubGraph)
            {
                m_ExposedToogle = new Toggle();
                m_ExposedToogle.OnToggleChanged(evt =>
                {
                    m_Graph.owner.RegisterCompleteObjectUndo("Change Exposed Toggle");
                    input.generatePropertyBlock = evt.newValue;
                    m_BlackboardField.icon = input.generatePropertyBlock ? BlackboardProvider.exposedIcon : null;
                    Rebuild();
                    DirtyNodes(ModificationScope.Graph);
                });
                m_ExposedToogle.value = input.generatePropertyBlock;
                AddRow("Exposed", m_ExposedToogle, input.isExposable);
				
            }

            if(!m_Graph.isSubGraph || input is ShaderKeyword)
            {
                m_ReferenceNameField = new TextField(512, false, false, ' ') { isDelayed = true };
                m_ReferenceNameField.styleSheets.Add(Resources.Load<StyleSheet>("Styles/PropertyNameReferenceField"));
                m_ReferenceNameField.value = input.referenceName;
                m_ReferenceNameField.RegisterValueChangedCallback(evt =>
                {
                    m_Graph.owner.RegisterCompleteObjectUndo("Change Reference Name");
                    if (m_ReferenceNameField.value != m_Input.referenceName)
                        m_Graph.SanitizeGraphInputReferenceName(input, evt.newValue);
                    
                    m_ReferenceNameField.value = input.referenceName;
                    if (string.IsNullOrEmpty(input.overrideReferenceName))
                        m_ReferenceNameField.RemoveFromClassList("modified");
                    else
                        m_ReferenceNameField.AddToClassList("modified");

                    Rebuild();
                    DirtyNodes(ModificationScope.Graph);
                    UpdateReferenceNameResetMenu();
                });
                if (!string.IsNullOrEmpty(input.overrideReferenceName))
                    m_ReferenceNameField.AddToClassList("modified");

                AddRow("Reference", m_ReferenceNameField, input.isRenamable);
            }
        }

        public abstract void BuildCustomFields(ShaderInput input);
        public abstract void DirtyNodes(ModificationScope modificationScope = ModificationScope.Node);

        void UpdateReferenceNameResetMenu()
        {
            if (string.IsNullOrEmpty(m_Input.overrideReferenceName))
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
                    m_Input.overrideReferenceName = null;
                    m_ReferenceNameField.value = m_Input.referenceName;
                    m_ReferenceNameField.RemoveFromClassList("modified");
                    DirtyNodes(ModificationScope.Graph);
                }, DropdownMenuAction.AlwaysEnabled);
        }

        public VisualElement AddRow(string labelText, VisualElement control, bool enabled = true)
        {
            VisualElement rowView = CreateRow(labelText, control, enabled);
            Add(rowView);
            m_Rows.Add(rowView);
            return rowView;
        }

        public void Rebuild()
        {
            // Delete all rows
            for (int i = 0; i < m_Rows.Count; i++)
            {
                if (m_Rows[i].parent == this)
                    Remove(m_Rows[i]);
            }

            // Rebuild
            BuildDefaultFields(m_Input);
            BuildCustomFields(m_Input);
        }

        VisualElement CreateRow(string labelText, VisualElement control, bool enabled)
        {
            VisualElement rowView = new VisualElement();

            rowView.AddToClassList("rowView");

            if(!string.IsNullOrEmpty(labelText))
            {
                Label label = new Label(labelText);
                label.SetEnabled(enabled);
                label.AddToClassList("rowViewLabel");
                rowView.Add(label);
            }
            
            control.AddToClassList("rowViewControl");
            control.SetEnabled(enabled);

            rowView.Add(control);
            return rowView;
        }
    }
}
