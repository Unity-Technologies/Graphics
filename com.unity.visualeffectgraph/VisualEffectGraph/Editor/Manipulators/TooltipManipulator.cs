using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    public class TooltipManipulator : IManipulate
    {
        public delegate string[] GetTooltipCallback();
        private Vector2 m_Position;
        private string[] m_Text;
        private GetTooltipCallback m_Callback;
        private bool m_bVisible;
        private Canvas2D m_Canvas;
        private PopupWindow m_PopupWindow;
        public TooltipManipulator(GetTooltipCallback callback)
        {
            m_bVisible = false;
            m_Callback = callback;
        }

        public void AttachTo(CanvasElement e)
        {
            e.MouseDown += E_MouseDown;
            e.MouseUp += E_MouseUp;

        }

        public void ShowTooltip(Canvas2D canvas)
        {
            m_bVisible = true;
            m_Text = m_Callback.Invoke();
            canvas.OnOverlay += RenderTooltip;
            canvas.MouseUp += E_MouseUp;
            canvas.AllEvents += E_MouseUpOutsideClientArea;
            canvas.Invalidate();
        }

        public void HideTooltip(Canvas2D canvas)
        {
            m_bVisible = false;
            canvas.OnOverlay -= RenderTooltip;
            canvas.MouseUp -= E_MouseUp;
            canvas.AllEvents -= E_MouseUpOutsideClientArea;
            canvas.Invalidate();
        }

        private bool E_MouseUpOutsideClientArea(CanvasElement element, Event e, Canvas2D parent)
        {
            if(!parent.clientRect.Contains(e.mousePosition))
            {
                HideTooltip(parent);
                e.Use();
                return true;
            }
            return false;
        }

        private bool E_MouseUp(CanvasElement element, Event e, Canvas2D parent)
        {
           if (e.button != 2)
                return false;
            HideTooltip(parent);
            e.Use();
            return true;
        }

        private bool E_MouseDown(CanvasElement element, Event e, Canvas2D parent)
        {
            if (e.button != 2)
                return false;

            ShowTooltip(parent);
            e.Use();
            return true;
        }

        public bool GetCaps(ManipulatorCapability cap)
        {
            return false;
        }

        private bool RenderTooltip(CanvasElement element, Event e, Canvas2D parent)
        {
            Color backup = GUI.color;
            int numlines = m_Text.Length;
            int lineheight = 14;
            

            GUI.color = Color.white;
            Vector2 position = e.mousePosition;
            Vector2 size = new Vector2(400, numlines * VFXEditor.styles.TooltipText.lineHeight + 24);

            Rect rect = new Rect(position + new Vector2(-12,-12), size);
            GUI.Box(rect, "", VFXEditor.styles.Tooltip);
            Rect currentLineRect = new Rect(position.x, position.y, size.x - 24, lineheight);

            foreach(string s in m_Text)
            {
                if(s != "")
                    GUI.Label(currentLineRect, s, VFXEditor.styles.TooltipText);

                currentLineRect.y += lineheight;
            }
            
            //Handles.DrawSolidRectangleWithOutline(r, Color.black, Color.white);

            GUI.color = backup;
            return true;
        }


    }
}
