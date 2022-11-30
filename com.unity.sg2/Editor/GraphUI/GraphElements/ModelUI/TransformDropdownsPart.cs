using System;
using Unity.GraphToolsFoundation.Editor;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class TransformDropdownsPart : BaseModelViewPart
    {
        VisualElement m_Root;
        EnumField m_FromDropdown, m_ToDropdown;
        Label m_Label;

        public override VisualElement Root => m_Root;

        public TransformDropdownsPart(string name, Model model, ModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName)
        {
        }

        protected override void BuildPartUI(VisualElement parent)
        {
            m_Root = new VisualElement();

            m_FromDropdown = new EnumField(default(CoordinateSpace));
            m_FromDropdown.RegisterValueChangedCallback(MakeFieldCallback(GraphDelta.TransformNode.kSourceSpace));

            m_ToDropdown = new EnumField(default(CoordinateSpace));
            m_ToDropdown.RegisterValueChangedCallback(MakeFieldCallback(GraphDelta.TransformNode.kDestinationSpace));

            m_Root.Add(m_FromDropdown);
            m_Root.Add(m_ToDropdown);

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
        }
    }
}

