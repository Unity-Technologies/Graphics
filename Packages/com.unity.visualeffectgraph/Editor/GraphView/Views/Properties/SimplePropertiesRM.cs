using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using EnumField = UnityEditor.VFX.UIElements.VFXEnumField;

namespace UnityEditor.VFX.UI
{
    class EnumPropertyRM : SimplePropertyRM<int>
    {
        public EnumPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        public override float GetPreferredControlWidth()
        {
            int min = 120;
            foreach (var str in Enum.GetNames(provider.portType))
            {
                Vector2 size = m_Field.Q<TextElement>().MeasureTextSize(str, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);

                size.x += 60;
                if (min < size.x)
                    min = (int)size.x;
            }
            if (min > 200)
                min = 200;


            return min;
        }

        public override ValueControl<int> CreateField()
        {
            var field = new EnumField(m_Label, m_Provider.portType);
            field.OnDisplayMenu = OnDisplayMenu;

            return field;
        }

        void OnDisplayMenu(EnumField field)
        {
            field.filteredOutValues = provider.filteredOutEnumerators;
        }
    }

    class Matrix4x4PropertyRM : SimpleVFXUIPropertyRM<VFXMatrix4x4Field, Matrix4x4>
    {
        public Matrix4x4PropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
            m_FieldParent.style.flexDirection = FlexDirection.Row;

            fieldControl.onValueDragFinished = () => ValueDragFinished();
            fieldControl.onValueDragStarted = () => ValueDragStarted();
        }

        public override float GetPreferredControlWidth()
        {
            return 260;
        }

        protected void ValueDragFinished()
        {
            m_Provider.EndLiveModification();
            hasChangeDelayed = false;
            NotifyValueChanged();
        }

        protected void ValueDragStarted()
        {
            m_Provider.StartLiveModification();
        }
    }

    class FlipBookPropertyRM : SimpleVFXUIPropertyRM<VFXFlipBookField, FlipBook>
    {
        public FlipBookPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        public override float GetPreferredControlWidth()
        {
            return 100;
        }
    }
}
