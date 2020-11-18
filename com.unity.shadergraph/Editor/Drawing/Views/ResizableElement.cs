using System.Collections.Generic;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    class ResizableElementFactory : UxmlFactory<ResizableElement>
    {}

    class ResizableElement : VisualElement
    {
        Dictionary<Resizer, VisualElement> m_Resizers = new Dictionary<Resizer, VisualElement>();

        List<Manipulator> m_Manipulators = new List<Manipulator>();

        public ResizableElement() : this("uxml/Resizable")
        {
            pickingMode = PickingMode.Ignore;
        }

        public ResizableElement(string uiFile)
        {
            var tpl = Resources.Load<VisualTreeAsset>(uiFile);
            var sheet = Resources.Load<StyleSheet>("Resizable");
            styleSheets.Add(sheet);

            tpl.CloneTree(this);

            foreach (Resizer value in System.Enum.GetValues(typeof(Resizer)))
            {
                VisualElement resizer = this.Q(value.ToString().ToLower() + "-resize");
                if (resizer != null)
                {
                    var manipulator = new ElementResizer(this, value);
                    resizer.AddManipulator(manipulator);
                    m_Manipulators.Add(manipulator);
                }
                m_Resizers[value] = resizer;
            }

            foreach (Resizer vertical in new[] {Resizer.Top, Resizer.Bottom})
                foreach (Resizer horizontal in new[] {Resizer.Left, Resizer.Right})
                {
                    VisualElement resizer = this.Q(vertical.ToString().ToLower() + "-" + horizontal.ToString().ToLower() + "-resize");
                    if (resizer != null)
                    {
                        var manipulator = new ElementResizer(this, vertical | horizontal);
                        resizer.AddManipulator(manipulator);
                        m_Manipulators.Add(manipulator);
                    }
                    m_Resizers[vertical | horizontal] = resizer;
                }
        }

        public enum Resizer
        {
            Top =           1 << 0,
            Bottom =        1 << 1,
            Left =          1 << 2,
            Right =         1 << 3,
        }

        // Lets visual element owners bind a callback to when any resize operation is completed
        public void BindOnResizeCallback(EventCallback<MouseUpEvent> mouseUpEvent)
        {
            foreach (var manipulator in m_Manipulators)
            {
                if (manipulator == null)
                    return;
                manipulator.target.RegisterCallback(mouseUpEvent);
            }
        }
    }

}
