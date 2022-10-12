using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolsFoundation.Editor;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class FunctionSelectorPart : BaseModelViewPart
    {
        private static readonly string ROOT_CLASS_NAME = "sg-function-selector-part";
        public override VisualElement Root => m_rootVisualElement;
        private readonly GraphDataNodeModel m_graphDataNodeModel;
        private VisualElement m_rootVisualElement;
        private DropdownField m_dropdownField;
        private int m_selectedFunctionIdx;
        private readonly List<string> m_functionNames;
        private readonly List<string> m_displayNames;
        private readonly string m_label;

        public FunctionSelectorPart(
            string name,
            GraphElementModel model,
            ModelView ownerElement,
            string parentClassName,
            string selectedFunctionName,
            IReadOnlyDictionary<string, string> options,
            string label = ""): base(name, model, ownerElement, parentClassName)
        {
            m_graphDataNodeModel = model as GraphDataNodeModel;
            m_functionNames = options.Keys.ToList();
            m_displayNames = options.Values.ToList();
            m_selectedFunctionIdx = m_functionNames.IndexOf(selectedFunctionName);
            m_label = label;
        }

        protected override void BuildPartUI(VisualElement parent)
        {
            m_rootVisualElement = new VisualElement();
            GraphElementHelper.LoadTemplateAndStylesheet(
                container: m_rootVisualElement,
                name: "FunctionSelectorPart",
                rootClassName: ROOT_CLASS_NAME);
            m_dropdownField = m_rootVisualElement.Q<DropdownField>("function-selector-part");
            if (!String.IsNullOrEmpty(m_label))
            {
                m_dropdownField.label = m_label;
            }
            m_dropdownField.choices = m_displayNames;
            m_dropdownField.index = m_selectedFunctionIdx;
            m_dropdownField.RegisterCallback<ChangeEvent<string>>(HandleSelectionChange);
            parent.Add(m_rootVisualElement);
        }

        protected override void UpdatePartFromModel()
        {
            // This Part is not effected by updates from the model.
        }

        private void HandleSelectionChange(ChangeEvent<string> evt)
        {
            int previousIndex = m_selectedFunctionIdx;
            int newIndex = m_dropdownField.index;
            string newFunctionName = m_functionNames[newIndex];
            string previousFunctionName = m_functionNames[previousIndex];
            var cmd = new ChangeNodeFunctionCommand(
                m_graphDataNodeModel,
                newFunctionName,
                previousFunctionName
            );
            m_OwnerElement.RootView.Dispatch(cmd);
            m_selectedFunctionIdx = newIndex;
        }
    }
}
