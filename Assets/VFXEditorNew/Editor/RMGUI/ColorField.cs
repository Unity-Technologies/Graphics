using UnityEngine;
using UnityEngine.RMGUI;
using UnityEditor.RMGUI;
using UnityEngine.RMGUI.StyleEnums;


namespace UnityEditor.VFX.RMGUI
{
    class ColorField : ValueControl<Color>
    {
        VisualElement m_Label;

        VisualElement m_ColorDisplay;
        
        VisualElement m_AlphaDisplay;
        VisualElement m_NotAlphaDisplay;

        VisualElement m_HDRLabel;

        VisualContainer m_Container;

        VisualContainer CreateColorContainer()
        {
            m_Container = new VisualContainer();

            m_Container.flexDirection = FlexDirection.Column;
            m_Container.alignItems = Align.Stretch;
            m_Container.AddToClassList("colorcontainer");

            m_ColorDisplay = new VisualElement(){height=10};
            m_ColorDisplay.AddToClassList("colordisplay");
            
            m_AlphaDisplay = new VisualElement(){height=3,backgroundColor=Color.white};
            m_AlphaDisplay.AddToClassList("alphadisplay");

            m_NotAlphaDisplay = new VisualElement() {height=3,backgroundColor=Color.black};
            m_NotAlphaDisplay.AddToClassList("notalphadisplay");

            VisualContainer alphaContainer = new VisualContainer() { flexDirection = FlexDirection.Row, height = 3 };

            m_ColorDisplay.AddManipulator(new Clickable(OnColorClick));
            m_AlphaDisplay.AddManipulator(new Clickable(OnColorClick));


            m_HDRLabel = new VisualElement(){text="HDR",
                                            textAlignment=TextAnchor.MiddleCenter,
                                            pickingMode = PickingMode.Ignore,
                                            positionType = PositionType.Absolute,
                                            positionTop=0,
                                            positionBottom=0,
                                            positionLeft=0,
                                            positionRight=0};
            m_HDRLabel.AddToClassList("hdr");

            m_Container.AddChild(m_ColorDisplay);
            m_Container.AddChild(alphaContainer);
            m_Container.AddChild(m_HDRLabel);

            alphaContainer.AddChild(m_AlphaDisplay);
            alphaContainer.AddChild(m_NotAlphaDisplay);


            return m_Container;
        }

        void OnColorClick()
        {
            ColorPicker.Show((System.Action < Color > )OnColorChanged, m_Value, true, true, new ColorPickerHDRConfig(0.0f, 100.0f, 0.0f, 100.0f));
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
        }

        void OnColorChanged(Color color)
        {
            SetValue(color);

            if (onValueChanged != null)
                onValueChanged();

            Dirty(ChangeType.Repaint);

        }

        protected override void ValueToGUI()
        {
            m_ColorDisplay.backgroundColor = new Color(m_Value.r,m_Value.g,m_Value.b,1);
            m_AlphaDisplay.flex = m_Value.a;
            m_NotAlphaDisplay.flex = 1-m_Value.a;

            bool hdr = m_Value.r > 1 || m_Value.g > 1 || m_Value.b > 1;
            if( (m_HDRLabel.parent != null) != hdr )
            {
                if( hdr )
                    m_Container.AddChild(m_HDRLabel);
                else
                    m_Container.RemoveChild(m_HDRLabel);
            }
        }

    }
}