using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.VFX;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal class VFXEdComment : CanvasElement, VFXModelHolder
    {

        private VFXCommentModel m_Model;
        private bool m_bEdit;

        internal VFXEdComment(VFXCommentModel model) 
            : base()
        {
            m_Model = model;
            translation = model.UIPosition;
            scale = model.UISize;
            m_ZIndex = -1000;

            AddManipulator(new Draggable());
            AddManipulator(new NodeDelete());
            AddManipulator(new CommentResize(new Vector2(400, 300)));
            AddManipulator(new ImguiContainer());
            m_bEdit = false;

            DoubleClick += VFXEdComment_DoubleClick;
        }

        private bool VFXEdComment_DoubleClick(CanvasElement element, Event e, Canvas2D parent)
        {
            if(!m_bEdit)
            {
                m_bEdit = true;
                e.Use();
                return true;
            }
            return false;
        }

        public VFXElementModel GetAbstractModel()
        {
            return m_Model;
        }

        public void OnRemoved()
        {
            m_Model.Detach();
        }

        public override void UpdateModel(UpdateType t)
        {
            m_Model.UIPosition = translation;
            m_Model.UISize = scale;
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            Rect r = GetDrawableRect();
            Color c = GUI.color;
            Color currentColor = m_Model.Color;

            if (selected)
                currentColor.a = 0.85f;
            else
                currentColor.a = 0.65f;

            GUI.color = currentColor;

            GUI.Box(r, GUIContent.none, VFXEditor.styles.Comment);
            GUI.color = c;

            Rect titlerect = new Rect(r.x + 24, r.y + 12, r.width - 48, 80);
            Rect bodyrect = new Rect(r.x + 24, r.y + 92, r.width - 48, r.height - 142);
            Rect cornerRect = new Rect(r.x + r.width - 40, r.y + r.height - 40, 32, 32);
            if(!m_bEdit)
            {
                GUI.Label(titlerect, m_Model.Title, VFXEditor.styles.CommentTitle);
                GUI.Label(bodyrect, m_Model.Body, VFXEditor.styles.CommentBody);
                GUI.Box(cornerRect, GUIContent.none, VFXEditor.styles.CommentResize);
            }
            else
            {
                m_Model.Title = GUI.TextField(titlerect, m_Model.Title, VFXEditor.styles.CommentTitleEdit);
                m_Model.Body = GUI.TextArea(bodyrect, m_Model.Body, VFXEditor.styles.CommentBodyEdit);
                Rect buttonRect = new Rect(r.x + r.width - 104, r.y + r.height - 40, 80, 24);

                if(GUI.Button(buttonRect, "OK"))
                {
                    m_bEdit = false;
                }

            }

        }

    }
}
