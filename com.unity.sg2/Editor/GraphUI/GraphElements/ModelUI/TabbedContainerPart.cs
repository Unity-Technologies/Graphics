using Unity.GraphToolsFoundation.Editor;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class TabbedContainerPart : BaseModelViewPart
    {
        VisualElement m_Root;
        public override VisualElement Root => m_Root;

        VisualElement m_PageR, m_PageG, m_PageB;

        public TabbedContainerPart(string name, Model model, ModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) { }

        private VisualElement MakeSliderPage()
        {
            var v = new VisualElement();
            v.Add(new Slider("R", -2, 2) { showInputField = true });
            v.Add(new Slider("G", -2, 2) { showInputField = true });
            v.Add(new Slider("B", -2, 2) { showInputField = true });
            return v;
        }

        protected override void BuildPartUI(VisualElement parent)
        {
            m_Root = new VisualElement();

            var tabs = new TabbedView();

            m_PageR = MakeSliderPage();
            m_PageG = MakeSliderPage();
            m_PageB = MakeSliderPage();

            tabs.AddTab(new TabButton("R", m_PageR), true);
            tabs.AddTab(new TabButton("G", m_PageG), false);
            tabs.AddTab(new TabButton("B", m_PageB), false);

            m_Root.Add(tabs);
            parent.Add(m_Root);
        }

        protected override void UpdatePartFromModel()
        {
        }
    }
}
