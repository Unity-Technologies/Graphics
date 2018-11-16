using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    class GradientEdge : Edge
    {
        readonly CustomStyleProperty<Color> k_InputColorProperty = new CustomStyleProperty<Color>("--edge-input-color");
        readonly CustomStyleProperty<Color> k_OutputColorProperty = new CustomStyleProperty<Color>("--edge-output-color");

        Color m_InputColor;
        Color m_OutputColor;

        public Color inputColor
        {
            get { return m_InputColor; }
        }

        public Color outputColor
        {
            get { return m_OutputColor; }
        }

        public GradientEdge()
        {
            m_InputColor = defaultColor;
            m_OutputColor = defaultColor;

            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }


        public void UpdateClasses(ConcreteSlotValueType outputType, ConcreteSlotValueType inputType)
        {
            ClearClassList();
            AddToClassList("edge");
            AddToClassList("from" + outputType);
            AddToClassList("to" + inputType);
        }

        private void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            Color inputValue;
            Color outputValue;

            ICustomStyle styles = e.customStyle;
            if (styles.TryGetValue(k_InputColorProperty, out inputValue))
                m_InputColor = inputValue;

            if (styles.TryGetValue(k_OutputColorProperty, out outputValue))
                m_OutputColor = outputValue;
        }

        protected override void DrawEdge()
        {
            if (!UpdateEdgeControl())
                return;

            edgeControl.edgeWidth = edgeWidth;
            edgeControl.inputColor = isGhostEdge ? ghostColor : (selected ? selectedColor : inputColor);
            edgeControl.outputColor = isGhostEdge ? ghostColor : (selected ? selectedColor : outputColor);
//            edgeControl.startCapColor = edgeControl.outputColor;
//            edgeControl.endCapColor = edgeControl.inputColor;
        }
    }
}
