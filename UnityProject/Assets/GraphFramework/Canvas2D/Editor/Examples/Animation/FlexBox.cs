using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO.Pipes;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor.Animations;
using UnityEditor.Experimental;
using UnityEditorInternal;
using UnityEngine.Experimental.Director;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    class ColoredBlockUI : CanvasElement
    {
        Color m_Color;
        public float m_Alpha = 0.0f;
        public ColoredBlockUI(float height)
        {
            m_Scale = new Vector3(0.0f, height, 0.0f);
            m_Color = UnityEngine.Random.ColorHSV();
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            Color c = GUI.color;
            GUI.color = new Color(1.0f, 1.0f, 1.0f, m_Alpha);
            EditorGUI.DrawRect(new Rect(0, translation.y, scale.x, scale.y), m_Color);
            GUI.color = c;
        }
    }

    class FlexBox : MoveableBox
    {
        private float m_FooterHeight = 15;
        private float m_ButtonWidth = 80.0f;

        public FlexBox(Vector2 position, float size)
            : base(position, size)
        {
            m_Title = "flexbox";
            AddManipulator(new Draggable());
            AddManipulator(new Resizable(new Vector2(m_ButtonWidth + 4.0f, 100.0f)));
            AddManipulator(new ImguiContainer());
        }

        public override void Layout()
        {
            float height = 50;
            for (int c = 0; c < m_Children.Count; c++)
            {
                var tx = m_Children[c].translation;
                tx.y = height;
                m_Children[c].translation = tx;
                var s = m_Children[c].scale;
                s.x = scale.x;
                m_Children[c].scale = s;
                height += m_Children[c].boundingRect.height;
            }
            height += m_FooterHeight;
            scale = new Vector2(scale.x, height);
            base.Layout();
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            var currentEvent = Event.current.type;
            base.Render(parentRect, canvas);

            if (GUI.Button(new Rect((m_Scale.x / 2.0f) - (m_ButtonWidth / 2.0f), 22, m_ButtonWidth, 20), "grow"))
            {
                ColoredBlockUI nb = new ColoredBlockUI(UnityEngine.Random.Range(30.0f, 78.0f));
                AddChild(nb);

                canvas.Animate(nb)
                .Lerp("m_Scale", new Vector3(scale.x, 0.0f, 0.0f), new Vector3(scale.x, nb.scale.y, 0.0f))
                .Lerp("m_Alpha", 0.0f, 1.0f)
                .Then((elem, anim, userData) =>
                {
                    anim.Done();
                });

                Invalidate();
                canvas.Repaint();
            }

            EditorGUI.DrawRect(new Rect(canvas.CanvasToScreen(boundingRect.center), new Vector2(2.0f, 2.0f)), Color.red);
        }
    }
}
