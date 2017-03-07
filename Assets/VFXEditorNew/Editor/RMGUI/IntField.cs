using UnityEngine;
using UnityEngine.RMGUI;
using UnityEditor.RMGUI;
using UnityEngine.RMGUI.StyleEnums;


namespace UnityEditor.VFX.RMGUI
{

    class IntField : ValueControl<int>, IValueChangeListener<int>
    {
        EditorTextField m_TextField;
        VisualElement m_Label;


        void CreateTextField()
        {
            m_TextField = new EditorTextField(30,false,false,'*');
            m_TextField.AddToClassList("textfield");
            m_TextField.onTextChanged = OnTextChanged;
            m_TextField.useStylePainter = true;
        }

        public IntField(string label) 
        {
            CreateTextField();            

            if( !string.IsNullOrEmpty(label) )
            {
                m_Label = new VisualElement(){text = label};
                m_Label.AddToClassList("label");
                m_Label.AddManipulator(new DragValueManipulator<int>(this,null));
                AddChild(m_Label);
            }

            flexDirection = FlexDirection.Row;
            AddChild(m_TextField);
        }

        public IntField(VisualElement existingLabel)
        {
            CreateTextField();
            AddChild(m_TextField);

            m_Label = existingLabel;

            m_Label.AddManipulator(new DragValueManipulator<int>(this, null));
        }

        void OnTextChanged()
        {
            m_Value = 0;
            int.TryParse(m_TextField.text, out m_Value);

            if(onValueChanged != null)
            {
                onValueChanged();
            }
        }


        int IValueChangeListener<int>.GetValue(object userData)
        {
            int newValue = 0;

            int.TryParse(m_TextField.text,out newValue);

            return newValue;
        }

        void IValueChangeListener<int>.SetValue(int value,object userData)
        {
            m_Value = value;
            ValueToGUI();

            if(onValueChanged != null)
            {
                onValueChanged();
            }
        }

        protected override void ValueToGUI()
        {
              m_TextField.text = m_Value.ToString("0.###");
        }

    }
}