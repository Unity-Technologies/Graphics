using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal class Watermark : IManipulate
    {
        Texture m_texture;

        public Watermark(Texture texture)
        {
            m_texture = texture;
        }

        public void AttachTo(CanvasElement element)
        {
            if (element is Canvas2D)
            {
                (element as Canvas2D).OnBackground += DrawWaterMark;
            }
        }

        public bool GetCaps(ManipulatorCapability cap)
        {
            return false;
        }

        private bool DrawWaterMark(CanvasElement element, Event e, Canvas2D parent)
        {
            Rect rect = new Rect(parent.clientRect.xMax - m_texture.width, parent.clientRect.yMax - m_texture.height, m_texture.width, m_texture.height);
            GUI.DrawTexture(rect,m_texture);
            return false;
        }
    }
}
