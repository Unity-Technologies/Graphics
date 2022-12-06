using Unity.GraphToolsFoundation.Editor;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class TabbedContainerPart : BaseModelViewPart
    {
        const float k_SliderMin = -2, k_SliderMax = 2;

        VisualElement m_Root;
        public override VisualElement Root => m_Root;

        VisualElement m_PageR, m_PageG, m_PageB;

        public TabbedContainerPart(string name, Model model, ModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) { }

        VisualElement MakeSliderPage(string portName)
        {
            var v = new VisualElement();
            if (m_Model is not GraphDataNodeModel graphDataNodeModel) return v;

            var rSlider = new Slider("R", k_SliderMin, k_SliderMax) {name = "slider-r", showInputField = true};
            var gSlider = new Slider("G", k_SliderMin, k_SliderMax) {name = "slider-g", showInputField = true};
            var bSlider = new Slider("B", k_SliderMin, k_SliderMax) {name = "slider-b", showInputField = true};

            rSlider.RegisterValueChangedCallback(c =>
            {
                m_OwnerElement.RootView.Dispatch(new SetGraphTypeValueCommand(graphDataNodeModel, portName, GraphType.Length.Three, GraphType.Height.One, c.newValue, gSlider.value, bSlider.value));
            });

            gSlider.RegisterValueChangedCallback(c =>
            {
                m_OwnerElement.RootView.Dispatch(new SetGraphTypeValueCommand(graphDataNodeModel, portName, GraphType.Length.Three, GraphType.Height.One, rSlider.value, c.newValue, bSlider.value));
            });

            bSlider.RegisterValueChangedCallback(c =>
            {
                m_OwnerElement.RootView.Dispatch(new SetGraphTypeValueCommand(graphDataNodeModel, portName, GraphType.Length.Three, GraphType.Height.One, rSlider.value, gSlider.value, c.newValue));
            });

            v.Add(rSlider);
            v.Add(gSlider);
            v.Add(bSlider);

            return v;
        }

        static void UpdatePageValues(VisualElement page, Vector3 values)
        {
            page.Q<Slider>("slider-r")?.SetValueWithoutNotify(values.x);
            page.Q<Slider>("slider-g")?.SetValueWithoutNotify(values.y);
            page.Q<Slider>("slider-b")?.SetValueWithoutNotify(values.z);
        }

        protected override void BuildPartUI(VisualElement parent)
        {
            m_Root = new VisualElement();

            var tabs = new TabbedView();

            m_PageR = MakeSliderPage("Red");
            m_PageG = MakeSliderPage("Green");
            m_PageB = MakeSliderPage("Blue");

            tabs.AddTab(new TabButton("R", m_PageR), true);
            tabs.AddTab(new TabButton("G", m_PageG), false);
            tabs.AddTab(new TabButton("B", m_PageB), false);

            m_Root.Add(tabs);
            parent.Add(m_Root);
        }

        protected override void UpdatePartFromModel()
        {
            if (m_Model is not GraphDataNodeModel graphDataNodeModel) return;
            if (!graphDataNodeModel.TryGetNodeHandler(out var handler)) return;

            var rVec = GraphTypeHelpers.GetAsVec3(handler.GetPort("Red").GetTypeField());
            var gVec = GraphTypeHelpers.GetAsVec3(handler.GetPort("Green").GetTypeField());
            var bVec = GraphTypeHelpers.GetAsVec3(handler.GetPort("Blue").GetTypeField());

            UpdatePageValues(m_PageR, rVec);
            UpdatePageValues(m_PageG, gVec);
            UpdatePageValues(m_PageB, bVec);
        }
    }
}
