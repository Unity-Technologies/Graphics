using System;
using System.Text;
using Unity.GraphToolsFoundation.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    using SwizzleNode = UnityEditor.ShaderGraph.GraphDelta.SwizzleNode;

    /// <summary>
    /// SwizzleMaskField is a custom text field that only allows valid mask characters to be entered.
    /// </summary>
    class SwizzleMaskField : TextValueField<string>
    {
        static string FilterMaskField(string input)
        {
            var sb = new StringBuilder(4);

            foreach (var c in input)
            {
                if (SwizzleNode.kAllowedMaskComponents.Contains(c, StringComparison.InvariantCultureIgnoreCase))
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        class SwizzleMaskInput : TextValueInput
        {
            protected override string allowedCharacters => SwizzleNode.kAllowedMaskComponents + SwizzleNode.kAllowedMaskComponents.ToUpperInvariant();
            protected override string ValueToString(string value) => value;
            public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, string startValue)
            {
            }
        }

        public SwizzleMaskField(string label, int maxLength)
            : base(label, maxLength, new SwizzleMaskInput()) { }

        protected override string ValueToString(string value) => value;
        protected override string StringToValue(string str) => FilterMaskField(str);
        public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, string startValue)
        {
        }
    }

    class SwizzleMaskPart : BaseModelViewPart
    {
        SwizzleMaskField m_MaskField;
        public override VisualElement Root => m_MaskField;

        public SwizzleMaskPart(string name, Model model, ModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) { }

        protected override void BuildPartUI(VisualElement parent)
        {
            m_MaskField = new SwizzleMaskField("Mask", 4);
            m_MaskField.tooltip = "a combination of one to four characters that can be x, y, z, w (or r, g, b, a)";
            m_MaskField.isDelayed = true;
            m_MaskField.RegisterValueChangedCallback(e =>
            {
                if (m_Model is not GraphDataNodeModel sgNodeModel) return;
                m_OwnerElement.RootView.Dispatch(new SetSwizzleMaskCommand(sgNodeModel, SwizzleNode.kMask, e.newValue ?? string.Empty));
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
            var field = handler.GetField<string>(SwizzleNode.kMask);
            m_MaskField.SetValueWithoutNotify(field?.GetData() ?? SwizzleNode.kDefaultMask);
        }
    }
}
