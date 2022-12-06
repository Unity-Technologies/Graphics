using Unity.GraphToolsFoundation.Editor;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class ChannelMixerPart : BaseModelViewPart
    {
        const float k_SliderMin = -2, k_SliderMax = 2;

        VisualElement m_Root;
        public override VisualElement Root => m_Root;

        Slider m_SliderR, m_SliderG, m_SliderB;
        string m_CurrentChannel = "Red"; // TODO: WIP

        public ChannelMixerPart(string name, Model model, ModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) { }

        protected override void BuildPartUI(VisualElement parent)
        {
            if (m_Model is not GraphDataNodeModel graphDataNodeModel) return;

            m_Root = new VisualElement();

            var toggles = new ToggleButtonStrip("", new[] {"Red", "Green", "Blue"}) {labels = new[] {"R", "G", "B"}};
            toggles.RegisterValueChangedCallback(e =>
            {
                m_CurrentChannel = e.newValue;
                UpdatePartFromModel();
            });

            m_Root.Add(toggles);

            m_SliderR = new Slider("R", k_SliderMin, k_SliderMax) {name = "slider-r", showInputField = true};
            m_SliderG = new Slider("G", k_SliderMin, k_SliderMax) {name = "slider-g", showInputField = true};
            m_SliderB = new Slider("B", k_SliderMin, k_SliderMax) {name = "slider-b", showInputField = true};

            m_SliderR.RegisterValueChangedCallback(c =>
            {
                m_OwnerElement.RootView.Dispatch(new SetGraphTypeValueCommand(graphDataNodeModel, m_CurrentChannel, GraphType.Length.Three, GraphType.Height.One, c.newValue, m_SliderG.value, m_SliderB.value));
            });

            m_SliderG.RegisterValueChangedCallback(c =>
            {
                m_OwnerElement.RootView.Dispatch(new SetGraphTypeValueCommand(graphDataNodeModel, m_CurrentChannel, GraphType.Length.Three, GraphType.Height.One, m_SliderR.value, c.newValue, m_SliderB.value));
            });

            m_SliderB.RegisterValueChangedCallback(c =>
            {
                m_OwnerElement.RootView.Dispatch(new SetGraphTypeValueCommand(graphDataNodeModel, m_CurrentChannel, GraphType.Length.Three, GraphType.Height.One, m_SliderR.value, m_SliderG.value, c.newValue));
            });

            m_Root.Add(m_SliderR);
            m_Root.Add(m_SliderG);
            m_Root.Add(m_SliderB);

            parent.Add(m_Root);
        }

        protected override void UpdatePartFromModel()
        {
            if (m_Model is not GraphDataNodeModel graphDataNodeModel) return;
            if (!graphDataNodeModel.TryGetNodeHandler(out var handler)) return;

            var channelVec = GraphTypeHelpers.GetAsVec3(handler.GetPort(m_CurrentChannel).GetTypeField());
            m_SliderR.SetValueWithoutNotify(channelVec.x);
            m_SliderG.SetValueWithoutNotify(channelVec.y);
            m_SliderB.SetValueWithoutNotify(channelVec.z);
        }
    }
}
