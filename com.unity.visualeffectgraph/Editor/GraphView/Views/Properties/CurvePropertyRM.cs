using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements;
using Object = UnityEngine.Object;
using System.Reflection;
using System.Linq;

using MyCurveField = UnityEditor.VFX.UIElements.VFXLabeledField<UnityEditor.VFX.UI.VFXCurveField, UnityEngine.AnimationCurve>;

namespace UnityEditor.VFX.UI
{
    // temporary override until the fix goes to trunk
    class VFXCurveField : CurveField
    {
        public VFXCurveField() : base()
        {
        }

        /*
         public override void SetValueWithoutNotify(AnimationCurve newValue)
        {
            m_ValueNull = newValue == null;
            if (!m_ValueNull)
            {
                m_Value.keys = newValue.keys;
                m_Value.preWrapMode = newValue.preWrapMode;
                m_Value.postWrapMode = newValue.postWrapMode;
            }
            else
            {
                m_Value.keys = new Keyframe[0];
                m_Value.preWrapMode = WrapMode.Once;
                m_Value.postWrapMode = WrapMode.Once;
            }
            m_TextureDirty = true;
            CurveEditorWindow.curve = m_Value;

            IncrementVersion(VersionChangeType.Repaint);

            m_Content?.IncrementVersion(VersionChangeType.Repaint);
        }
         * */


        static FieldInfo s_m_ValueNull = typeof(CurveField).GetField("m_ValueNull", BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.Instance);
        static FieldInfo s_m_Value = typeof(CurveField).GetField("m_Value", BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.Instance);
        static FieldInfo s_m_TextureDirty = typeof(CurveField).GetField("m_TextureDirty", BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.Instance);
        static FieldInfo s_m_Content = typeof(CurveField).GetField("m_Content", BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.Instance);

        public override void SetValueWithoutNotify(AnimationCurve newValue)
        {
            s_m_ValueNull.SetValue(this, newValue == null);

            AnimationCurve mValue = (AnimationCurve)s_m_Value.GetValue(this);
            if (newValue != null)
            {
                mValue.keys = newValue.keys;
                mValue.preWrapMode = newValue.preWrapMode;
                mValue.postWrapMode = newValue.postWrapMode;
            }
            else
            {
                mValue.keys = new Keyframe[0];
                mValue.preWrapMode = WrapMode.Once;
                mValue.postWrapMode = WrapMode.Once;
            }

            s_m_TextureDirty.SetValue(this, true);

            if (value != null && CurveEditorWindow.visible && Object.ReferenceEquals(CurveEditorWindow.curve, m_Value))
            {
                CurveEditorWindow.curve = m_Value;
                CurveEditorWindow.instance.Repaint();
            }
            MarkDirtyRepaint();

            var content = ((VisualElement)s_m_Content.GetValue(this));
            if (content != null)
                content.MarkDirtyRepaint();
        }
    }
    class CurvePropertyRM : PropertyRM<AnimationCurve>
    {
        public CurvePropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
            m_CurveField = new MyCurveField(m_Label);
            m_CurveField.control.renderMode = CurveField.RenderMode.Mesh;
            m_CurveField.RegisterCallback<ChangeEvent<AnimationCurve>>(OnValueChanged);

            m_CurveField.style.flexDirection = FlexDirection.Column;
            m_CurveField.style.alignItems = Align.Stretch;
            m_CurveField.style.flex = new Flex(1, 0);

            Add(m_CurveField);
        }

        public override float GetPreferredControlWidth()
        {
            return 110;
        }

        public void OnValueChanged(ChangeEvent<AnimationCurve> e)
        {
            AnimationCurve newValue = m_CurveField.value;
            m_Value = newValue;
            NotifyValueChanged();
        }

        MyCurveField m_CurveField;

        protected override void UpdateEnabled()
        {
            m_CurveField.SetEnabled(propertyEnabled);
        }

        protected override void UpdateIndeterminate()
        {
            m_CurveField.visible = !indeterminate;
        }

        public override void UpdateGUI(bool force)
        {
            m_CurveField.SetValueWithoutNotify(m_Value);
        }

        public override bool showsEverything { get { return true; } }
    }
}
