using UnityEngine;
using UnityEngine.RMGUI;
using UnityEditor.RMGUI;
using UnityEngine.RMGUI.StyleEnums;


namespace UnityEditor.VFX.RMGUI
{
    class ColorEventListener : Manipulator
    {
        System.Action<Color> m_onColorChanged;

        public ColorEventListener(System.Action<Color> onColorChanged)
        {
            m_onColorChanged = onColorChanged;
        }
        public override EventPropagation HandleEvent(Event evt, VisualElement finalTarget)
		{
            if( evt.type == EventType.ExecuteCommand && evt.commandName == "ColorPickerChanged")
            {
                m_onColorChanged(ColorPicker.color);
                return EventPropagation.Stop;
            }

            return EventPropagation.Continue;
        }
    }

    class ColorField : ValueControl<Color>
    {
        VisualElement m_Label;

        VisualElement m_ColorDisplay;

        VisualElement m_AlphaDisplay;


        VisualContainer CreateColorContainer()
        {
            VisualContainer container = new VisualContainer();

            container.flexDirection = FlexDirection.Column;
            container.alignItems = Align.Stretch;
            container.AddToClassList("colorcontainer");

            m_ColorDisplay = new VisualElement(){height=10};
            m_ColorDisplay.AddToClassList("colordisplay");
            
            m_AlphaDisplay = new VisualElement(){height=3};
            m_AlphaDisplay.AddToClassList("alphadisplay");

            m_ColorDisplay.AddManipulator(new Clickable(OnColorClick));
            m_AlphaDisplay.AddManipulator(new Clickable(OnColorClick));

            container.AddChild(m_ColorDisplay);
            container.AddChild(m_AlphaDisplay);

            return container;
        }

        void OnColorClick()
        {
            ColorPicker.Show(GUIView.current, m_Value, true, true, new ColorPickerHDRConfig(0.0f, 100.0f, 0.0f, 100.0f));
        }

        public ColorField(string label) 
        {
            VisualContainer container = CreateColorContainer();            

            if( !string.IsNullOrEmpty(label) )
            {
                m_Label = new VisualElement(){text = label};
                m_Label.AddToClassList("label");
                AddChild(m_Label);
            }

            flexDirection = FlexDirection.Row;
            AddChild(container);
        }

        

        public ColorField(VisualElement existingLabel)
        {
            VisualContainer container = CreateColorContainer();     
            AddChild(container);

            m_Label = existingLabel;

            AddManipulator(new ColorEventListener(OnColorChanged));
        }

        void OnColorChanged(Color color)
        {
            SetValue(color);
        }

        protected override void ValueToGUI()
        {
              m_ColorDisplay.backgroundColor = new Color(m_Value.r,m_Value.g,m_Value.b,1);
              m_AlphaDisplay.backgroundColor = new Color(m_Value.a,m_Value.a,m_Value.a,1);
        }

    }
}