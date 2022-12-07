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
        static readonly string[] k_ChannelNames = { "Red", "Green", "Blue" };
        static readonly string[] k_ChannelLabels = { "R", "G", "B" };

        VisualElement m_Root;
        public override VisualElement Root => m_Root;

        Slider m_SliderR, m_SliderG, m_SliderB;
        string m_CurrentChannel;

        public ChannelMixerPart(string name, Model model, ModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) { }

        protected override void BuildPartUI(VisualElement parent)
        {
            if (m_Model is not SGNodeModel sgNodeModel) return;

            m_Root = new VisualElement();
            GraphElementHelper.LoadTemplateAndStylesheet(m_Root, "ChannelMixerPart", "sg-channel-mixer");

            // TODO: This is GTF's port of the toggle control. Replace with public UITK equivalent when available.
            var channelToggles = new ToggleButtonStrip(null, new[] {"Red", "Green", "Blue"}) {labels = new[] {"R", "G", "B"}};
            channelToggles.RegisterValueChangedCallback(e =>
            {
                m_CurrentChannel = e.newValue;
                UpdatePartFromModel();
            });

            m_CurrentChannel = k_ChannelNames[0];
            channelToggles.value = m_CurrentChannel;

            m_Root.Q("channel-toggle-container").Add(channelToggles);

            m_SliderR = m_Root.Q<Slider>("slider-r");
            m_SliderG = m_Root.Q<Slider>("slider-g");
            m_SliderB = m_Root.Q<Slider>("slider-b");

            EventCallback<ChangeEvent<float>> UpdateComponentCallback(int changedComponentIndex)
            {
                return e =>
                {
                    var values = new[] {m_SliderR.value, m_SliderG.value, m_SliderB.value};
                    values[changedComponentIndex] = e.newValue;

                    m_OwnerElement.RootView.Dispatch(new SetGraphTypeValueCommand(sgNodeModel, m_CurrentChannel, GraphType.Length.Three, GraphType.Height.One, values));
                };
            }

            m_SliderR.RegisterValueChangedCallback(UpdateComponentCallback(0));
            m_SliderG.RegisterValueChangedCallback(UpdateComponentCallback(1));
            m_SliderB.RegisterValueChangedCallback(UpdateComponentCallback(2));

            parent.Add(m_Root);
        }

        protected override void UpdatePartFromModel()
        {
            if (m_Model is not SGNodeModel sgNodeModel) return;
            if (!sgNodeModel.TryGetNodeHandler(out var handler)) return;

            var channelVec = GraphTypeHelpers.GetAsVec3(handler.GetPort(m_CurrentChannel).GetTypeField());
            m_SliderR.SetValueWithoutNotify(channelVec.x);
            m_SliderG.SetValueWithoutNotify(channelVec.y);
            m_SliderB.SetValueWithoutNotify(channelVec.z);
        }
    }
}
