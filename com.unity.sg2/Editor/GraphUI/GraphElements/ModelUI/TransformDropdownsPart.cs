using System;
using Unity.GraphToolsFoundation.Editor;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    using CoordinateSpace = UnityEditor.ShaderGraph.GraphDelta.CoordinateSpace;
    using ConversionType = UnityEditor.ShaderGraph.GraphDelta.ConversionType;

    class TransformDropdownsPart : BaseModelViewPart
    {
        VisualElement m_Root;
        EnumField m_FromDropdown, m_ToDropdown;
        EnumField m_ModeDropdown;
        Label m_Label;

        public override VisualElement Root => m_Root;

        public TransformDropdownsPart(string name, Model model, ModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) { }

        protected override void BuildPartUI(VisualElement parent)
        {
            m_Root = new VisualElement();

            GraphElementHelper.LoadTemplateAndStylesheet(m_Root, "TransformDropdownsPart", "");

            m_FromDropdown = m_Root.Q<EnumField>("from");
            m_FromDropdown.Init(default(CoordinateSpace));

            m_ToDropdown = m_Root.Q<EnumField>("to");
            m_ToDropdown.Init(default(CoordinateSpace));

            m_ModeDropdown = m_Root.Q<EnumField>("type");
            m_ModeDropdown.Init(default(ConversionType));

            m_FromDropdown.RegisterValueChangedCallback(MakeFieldCallback(GraphDelta.TransformNode.kSourceSpace));
            m_ToDropdown.RegisterValueChangedCallback(MakeFieldCallback(GraphDelta.TransformNode.kDestinationSpace));
            parent.Add(m_Root);
        }

        EventCallback<ChangeEvent<Enum>> MakeFieldCallback(string fieldName)
        {
            return e =>
            {
                if (m_Model is not GraphDataNodeModel sgNodeModel) return;
                m_OwnerElement.RootView.Dispatch(new SetCoordinateSpaceCommand(sgNodeModel, fieldName, (CoordinateSpace)e.newValue));
            };
        }

        protected override void UpdatePartFromModel()
        {
            if (m_Model is not GraphDataNodeModel sgNodeModel) return;
            if (!sgNodeModel.TryGetNodeHandler(out var handler)) return;

            var fromField = handler.GetField<CoordinateSpace>(GraphDelta.TransformNode.kSourceSpace);
            if (fromField != null) m_FromDropdown.SetValueWithoutNotify(fromField.GetData());

            var toField = handler.GetField<CoordinateSpace>(GraphDelta.TransformNode.kDestinationSpace);
            if (toField != null) m_ToDropdown.SetValueWithoutNotify(toField.GetData());

            var modeField = handler.GetField<ConversionType>(GraphDelta.TransformNode.kConversionType);
            if (modeField != null) m_ModeDropdown.SetValueWithoutNotify(modeField.GetData());
        }
    }
}
