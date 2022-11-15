using System.Collections.Generic;
using Unity.GraphToolsFoundation.Editor;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class ConversionDropdownsPart : BaseModelViewPart
    {
        VisualElement m_Root;
        DropdownField m_FromDropdown, m_ToDropdown;
        Label m_Label;

        public override VisualElement Root => m_Root;

        public ConversionDropdownsPart(string name, Model model, ModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) { }

        protected override void BuildPartUI(VisualElement parent)
        {
            m_Root = new VisualElement();

            // TODO: Install value changed callbacks. Also, maybe set up the VEs through a UXML template instead.
            m_FromDropdown = new DropdownField(new List<string> { "A1", "A2", "A3" }, 0);
            m_ToDropdown = new DropdownField(new List<string> { "B1", "B2", "B3" }, 0);

            // TODO: Styling (these dropdowns currently take up entire rows)
            m_Root.Add(m_FromDropdown);
            m_Root.Add(m_ToDropdown);

            parent.Add(m_Root);
        }

        protected override void UpdatePartFromModel()
        {
            // TODO: Update m_FromDropdown from field TransformNode.kSourceSpace
            // TODO: Update m_ToDropdown from field TransformNode.kDestinationSpace
        }
    }
}
