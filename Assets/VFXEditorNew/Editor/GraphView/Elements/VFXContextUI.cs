using System;
using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.RMGUI.StyleSheets;

namespace UnityEditor.VFX.UI
{
    class VFXContextUI : GraphElement
    {
        const string RectColorProperty = "rect-color";

        public VFXContextUI()
        {

        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();

            VFXContextDesc.Type contextType = GetPresenter<VFXContextPresenter>().Model.ContextType;

            RemoveFromClassList("init", "update", "output");

            switch (contextType)
            {
                case VFXContextDesc.Type.kTypeInit: AddToClassList("init"); break;
                case VFXContextDesc.Type.kTypeUpdate: AddToClassList("update"); break;
                case VFXContextDesc.Type.kTypeOutput: AddToClassList("output"); break;
                default: throw new Exception();
            }
        }

        public override void DoRepaint(IStylePainter painter)
        {
            painter.DrawRect(position, m_RectColor);
        }

        public override void OnStylesResolved(VisualElementStyles elementStyles)
		{
			base.OnStylesResolved(elementStyles);
			elementStyles.ApplyCustomProperty(RectColorProperty, ref m_RectColor); 
		}

        StyleProperty<Color> m_RectColor;
        Color rectColor { get { return m_RectColor.GetOrDefault(Color.magenta); } }

        //private Color m_Color;
    }
}
