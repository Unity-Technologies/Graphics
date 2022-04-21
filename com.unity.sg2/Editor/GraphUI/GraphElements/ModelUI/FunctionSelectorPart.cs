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
        private readonly Dictionary<string, string> m_choiceToKey;

        public FunctionSelectorPart(
            string name,
            IGraphElementModel model,
            IModelView ownerElement,
            string parentClassName,
            IReadOnlyDictionary<string, string> options) : base(name, model, ownerElement, parentClassName)
        {
            // Invert the options because the values are displayed.
            m_choiceToKey = new Dictionary<string, string>();
            foreach (var key in options.Keys)
            {
                m_choiceToKey[options[key]] = key;
            }
            
            m_graphDataNodeModel = model as GraphDataNodeModel;
        }

        protected override void BuildPartUI(VisualElement parent)
        {
            m_rootVisualElement = new VisualElement();
            GraphElementHelper.LoadTemplateAndStylesheet(
                container: m_rootVisualElement,
                name: "FunctionSelectorPart",
                rootClassName: ROOT_CLASS_NAME);
            var uxmlField = m_rootVisualElement.Q<DropdownField>("function-selector-part");
            uxmlField.choices = m_choiceToKey.Keys.ToList();

            // TODO (Brett) Change this to be the right selection
            uxmlField.value = uxmlField.choices[0];

            uxmlField.RegisterCallback<ChangeEvent<string>>(HandleSelectionChange);
            parent.Add(m_rootVisualElement);
        }

        protected override void UpdatePartFromModel()
        {
            // This Part is not effected by updates from the model.
        }

        private void HandleSelectionChange(ChangeEvent<string> evt)
        {
            string newValue = m_choiceToKey[evt.newValue];
            string previousValue = m_choiceToKey[evt.previousValue];
            var cmd = new ChangeNodeFunctionCommand(m_graphDataNodeModel, newValue, previousValue);
            m_OwnerElement.RootView.Dispatch(cmd);
        }
    }
}
