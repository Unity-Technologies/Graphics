using Unity.GraphToolsFoundation.Editor;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class SwizzleMaskPart : BaseModelViewPart
    {
        // TODO: Move constants to node builder
        const string k_MaskFieldName = "Mask";
        const string k_MaskDefaultValue = "xyzw";

        TextField m_MaskField;
        public override VisualElement Root => m_MaskField;

        public SwizzleMaskPart(string name, Model model, ModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) { }

        protected override void BuildPartUI(VisualElement parent)
        {
            m_MaskField = new TextField("Mask");
            m_MaskField.isDelayed = true;
            m_MaskField.RegisterValueChangedCallback(e =>
            {
                if (m_Model is not GraphDataNodeModel sgNodeModel) return;
                m_OwnerElement.RootView.Dispatch(new SetSwizzleMaskCommand(sgNodeModel, k_MaskFieldName, e.newValue));
            });

            parent.Add(m_MaskField);
        }

        protected override void UpdatePartFromModel()
        {
            if (m_Model is not GraphDataNodeModel sgNodeModel) return;
            if (!sgNodeModel.TryGetNodeHandler(out var handler)) return;

            // This is a field instead of a port because there's no real string anywhere at runtime -- it affects
            // the generated code like function dropdowns do (which also use fields).

            // TODO: Remove CLDS usage from view
            var field = handler.GetField<string>(k_MaskFieldName);
            m_MaskField.SetValueWithoutNotify(field?.GetData() ?? k_MaskDefaultValue);
        }
    }
}
