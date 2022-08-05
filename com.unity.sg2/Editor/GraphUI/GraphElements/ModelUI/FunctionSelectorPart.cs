using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class FunctionSelectorPart : BaseModelViewPart
    {
        public override VisualElement Root => m_rootVisualElement;
        private static readonly string ROOT_CLASS_NAME = "sg-function-selector-part";
        private readonly GraphDataNodeModel m_graphDataNodeModel;
        private VisualElement m_rootVisualElement;
        private DropdownField m_dropdownField;
        private int m_selectedFunctionIdx;
        private readonly List<string> functionNames;
        private readonly List<string> displayNames;

        public FunctionSelectorPart(
            string name,
            IGraphElementModel model,
            IModelView ownerElement,
            string parentClassName,
            string selectedFunctionName,
            IReadOnlyDictionary<string, string> options) : base(name, model, ownerElement, parentClassName)
        {
            m_graphDataNodeModel = model as GraphDataNodeModel;
            functionNames = options.Keys.ToList();
            displayNames = options.Values.ToList();
            m_selectedFunctionIdx = functionNames.IndexOf(selectedFunctionName);
        }

        protected override void BuildPartUI(VisualElement parent)
        {
            m_rootVisualElement = new VisualElement();
            GraphElementHelper.LoadTemplateAndStylesheet(
                container: m_rootVisualElement,
                name: "FunctionSelectorPart",
                rootClassName: ROOT_CLASS_NAME);
            m_dropdownField = m_rootVisualElement.Q<DropdownField>("function-selector-part");
            m_dropdownField.choices = displayNames;
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
            string newFunctionName = functionNames[newIndex];
            string previousFunctionName = functionNames[previousIndex];
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
