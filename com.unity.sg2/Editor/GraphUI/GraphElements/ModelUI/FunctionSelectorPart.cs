using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolsFoundation.Editor;
using UnityEditor.ShaderGraph.Defs;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class FunctionSelectorPart : BaseModelViewPart
    {
        private static readonly string ROOT_CLASS_NAME = "sg-function-selector-part";
        public override VisualElement Root => m_rootVisualElement;
        private readonly SGNodeModel m_sgNodeModel;
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
            m_sgNodeModel = model as SGNodeModel;
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
            if (!m_sgNodeModel.graphDataOwner.TryGetNodeHandler(out var reader))
                return; // (Brett) Should maybe log warning here
            var field = reader.GetField<string>(NodeDescriptorNodeBuilder.SELECTED_FUNCTION_FIELD_NAME);
            if (field == null)
                return;
            var data = field.GetData();
            m_dropdownField.SetValueWithoutNotify(data);
        }

        private void HandleSelectionChange(ChangeEvent<string> evt)
        {
            int previousIndex = m_selectedFunctionIdx;
            int newIndex = m_dropdownField.index;
            string newFunctionName = m_functionNames[newIndex];
            string previousFunctionName = m_functionNames[previousIndex];
            var cmd = new ChangeNodeFunctionCommand(
                m_sgNodeModel,
                newFunctionName,
                previousFunctionName
            );
            m_OwnerElement.RootView.Dispatch(cmd);
            m_selectedFunctionIdx = newIndex;
        }
    }
}
