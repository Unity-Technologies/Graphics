using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class FunctionSelectorPart : BaseModelViewPart
    {
        public override VisualElement Root => rootVisualElement;
        private static readonly string ROOT_CLASS_NAME = "sg-function-selector-part";
        private readonly GraphDataNodeModel graphDataNodeModel;
        private VisualElement rootVisualElement;
        private readonly Dictionary<string, string> choiceToKey;

        public FunctionSelectorPart(
            string name,
            IGraphElementModel model,
            IModelView ownerElement,
            string parentClassName,
            IReadOnlyDictionary<string, string> options) : base(name, model, ownerElement, parentClassName)
        {
            // Invert the options because the values are displayed.
            choiceToKey = new Dictionary<string, string>();
            foreach (var key in options.Keys)
            {
                choiceToKey[options[key]] = key;
            }
            
            graphDataNodeModel = model as GraphDataNodeModel;
        }

        protected override void BuildPartUI(VisualElement parent)
        {
            rootVisualElement = new VisualElement();
            GraphElementHelper.LoadTemplateAndStylesheet(
                container: rootVisualElement,
                name: "FunctionSelectorPart",
                rootClassName: ROOT_CLASS_NAME);
            var uxmlField = rootVisualElement.Q<DropdownField>("function-selector-part");
            uxmlField.choices = choiceToKey.Keys.ToList();
            uxmlField.value = uxmlField.choices[0];
            parent.Add(rootVisualElement);

            //m_Root = new VisualElement { name = PartName };
            //m_Root.AddStylesheet("StaticPortParts/SingleFieldPart.uss");
            //GraphElementHelper.LoadTemplate(m_Root, UXMLTemplateName);
            //m_Field = m_Root.Q<F>(FieldName);
            //m_Field.RegisterValueChangedCallback(OnFieldValueChanged);
            //if (m_Field is BaseField<T> baseField)
            //{
            //    baseField.label = m_PortName;
            //}
            //parent.Add(m_Root);
        }

        protected override void UpdatePartFromModel()
        {
            if (!graphDataNodeModel.existsInGraphData)
                return;
            Debug.LogWarning("UpdatePartFromModel called!");
            // throw new System.NotImplementedException();
        }
    }
}
