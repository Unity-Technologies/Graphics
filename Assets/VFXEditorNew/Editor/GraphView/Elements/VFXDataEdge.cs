using UnityEngine;
using RMGUI.GraphView;
using UnityEngine.RMGUI.StyleSheets;

namespace UnityEditor.VFX.UI
{
    public class VFXDataEdge : Edge
    {
        public VFXDataEdge()
        {
        }


        const string FloatColorProperty = "float-color";
        const string Vector2ColorProperty = "vector2-color";
        const string Vector3ColorProperty = "vector3-color";
        const string Vector4ColorProperty = "vector4-color";
        const string ColorColorProperty = "color-color";
        const string ObjectColorProperty = "object-color";

        StyleProperty<Color> m_FloatColor;
        StyleProperty<Color> m_Vector2Color;
        StyleProperty<Color> m_Vector3Color;
        StyleProperty<Color> m_Vector4Color;
        StyleProperty<Color> m_ColorColor;
        StyleProperty<Color> m_ObjectColor;

        public Color floatColor
        {
            get
            {
                return m_FloatColor.GetOrDefault(Color.white);
            }
        }
        public Color vector2Color
        {
            get
            {
                return m_Vector2Color.GetOrDefault(Color.white);
            }
        }
        public Color vector3Color
        {
            get
            {
                return m_Vector3Color.GetOrDefault(Color.white);
            }
        }
        public Color vector4Color
        {
            get
            {
                return m_Vector4Color.GetOrDefault(Color.white);
            }
        }
        public Color colorColor
        {
            get
            {
                return m_ColorColor.GetOrDefault(Color.white);
            }
        }
        public Color objectColor
        {
            get
            {
                return m_ObjectColor.GetOrDefault(Color.white);
            }
        }


        public override void OnStylesResolved(VisualElementStyles elementStyles)
        {
            base.OnStylesResolved(elementStyles);
            elementStyles.ApplyCustomProperty(FloatColorProperty, ref m_FloatColor);
            elementStyles.ApplyCustomProperty(Vector2ColorProperty, ref m_Vector2Color);
            elementStyles.ApplyCustomProperty(Vector3ColorProperty, ref m_Vector3Color);
            elementStyles.ApplyCustomProperty(Vector4ColorProperty, ref m_Vector4Color);
            elementStyles.ApplyCustomProperty(ColorColorProperty, ref m_ColorColor);
            elementStyles.ApplyCustomProperty(ObjectColorProperty, ref m_ObjectColor);
        }


        protected override void DrawEdge(IStylePainter painter)
		{
			var edgePresenter = GetPresenter<EdgePresenter>();

			NodeAnchorPresenter outputPresenter = edgePresenter.output;
			NodeAnchorPresenter inputPresenter = edgePresenter.input;

			if (outputPresenter == null && inputPresenter == null)
				return;

			Vector2 from = Vector2.zero;
			Vector2 to = Vector2.zero;
			GetFromToPoints(ref from, ref to);

            //Color edgeColor = edgePresenter.selected ? new Color(240/255f,240/255f,240/255f) : new Color(146/255f,146/255f,146/255f);

            System.Type type = inputPresenter != null ? inputPresenter.anchorType : outputPresenter.anchorType;


            Color edgeColor;
            if (typeof(float) == type)
            {
                edgeColor = floatColor;
            }
            else if (typeof(Vector2) == type)
            {
                edgeColor = vector2Color;
            }
            else if (typeof(Vector3) == type)
            {
                edgeColor = vector3Color;
            }
            else if (typeof(Vector4) == type)
            {
                edgeColor = vector4Color;
            }
            else if (typeof(Color) == type)
            {
                edgeColor = colorColor;
            }
            else
            {
                edgeColor = objectColor;
            }

            if( edgePresenter.selected )
            {
                edgeColor.r += 0.3f;
                edgeColor.g += 0.3f;
                edgeColor.b += 0.3f;
            }

            Orientation orientation = Orientation.Vertical;
			Vector3[] points, tangents;
			GetTangents(orientation, from, to, out points, out tangents);
			Handles.DrawBezier(points[0], points[1], tangents[0], tangents[1], edgeColor, null, 2f);

		}

    }
}
